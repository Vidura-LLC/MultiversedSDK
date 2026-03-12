using System;
using System.Collections;
using UnityEngine;
using Multiversed.Models;
using Multiversed.Utils;

namespace Multiversed.Core.Pay
{
    /// <summary>
    /// Implements YIP.pay() flow using Multiversed backend and Transak top-up when needed.
    /// Coroutine-based (no async/await).
    /// </summary>
    public class YIPPay
    {
        private readonly ApiClient _apiClient;
        private readonly SDKConfig _config;

        private const float POLL_INTERVAL_SEC = 3f;
        private const float POLL_TIMEOUT_SEC = 300f;
        private const int MAX_RETRY_ATTEMPTS = 3;
        private const float RETRY_DELAY_SEC = 2f;

        public YIPPay(ApiClient apiClient, SDKConfig config)
        {
            _apiClient = apiClient;
            _config = config;
        }

        public IEnumerator Pay(
            string userId,
            string contextType,
            string contextId,
            int tokenType,
            Action<PayResult> onComplete
        )
        {
            PayResponse payResponse = null;
            string payError = null;

            var request = new PayRequest
            {
                userId = userId,
                contextType = contextType,
                contextId = contextId,
                tokenType = tokenType
            };

            // STEP 1 — POST /api/sdk/pay
            yield return _apiClient.PostPay(request, (resp, err) =>
            {
                payResponse = resp;
                payError = err;
            });

            if (payResponse == null)
            {
                onComplete?.Invoke(new PayResult
                {
                    IsSuccess = false,
                    Status = "error",
                    ErrorMessage = !string.IsNullOrEmpty(payError) ? payError : "Failed to call pay endpoint"
                });
                yield break;
            }

            if (!payResponse.success || payResponse.status == "error")
            {
                onComplete?.Invoke(new PayResult
                {
                    IsSuccess = false,
                    Status = payResponse.status ?? "error",
                    ErrorMessage = payResponse.message ?? "Payment failed"
                });
                yield break;
            }

            // STEP 2 — Route on status
            if (payResponse.status == "completed")
            {
                onComplete?.Invoke(new PayResult
                {
                    IsSuccess = true,
                    Status = "completed",
                    TxSignature = payResponse.txSignature
                });
                yield break;
            }

            if (payResponse.status == "needs_signature")
            {
                onComplete?.Invoke(new PayResult
                {
                    IsSuccess = false,
                    Status = "needs_signature",
                    ErrorMessage = "External wallet signing required: " + payResponse.serializedTx
                });
                yield break;
            }

            if (payResponse.status != "awaiting_topup")
            {
                onComplete?.Invoke(new PayResult
                {
                    IsSuccess = false,
                    Status = payResponse.status,
                    ErrorMessage = "Unexpected status: " + payResponse.status
                });
                yield break;
            }

            if (string.IsNullOrEmpty(payResponse.topUpUrl))
            {
                onComplete?.Invoke(new PayResult
                {
                    IsSuccess = false,
                    Status = "awaiting_topup",
                    ErrorMessage = "Missing topUpUrl for awaiting_topup"
                });
                yield break;
            }

            if (string.IsNullOrEmpty(payResponse.intentId))
            {
                onComplete?.Invoke(new PayResult
                {
                    IsSuccess = false,
                    Status = "awaiting_topup",
                    ErrorMessage = "Missing intentId for awaiting_topup"
                });
                yield break;
            }

            Application.OpenURL(payResponse.topUpUrl);

            // STEP 3 — Polling loop
            float elapsed = 0f;
            int retryCount = 0;
            string currentIntentId = payResponse.intentId;

            while (elapsed < POLL_TIMEOUT_SEC)
            {
                yield return new WaitForSeconds(POLL_INTERVAL_SEC);
                elapsed += POLL_INTERVAL_SEC;

                PayStatusResponse statusResponse = null;
                string statusError = null;

                yield return _apiClient.GetPayStatus(currentIntentId, (resp, err) =>
                {
                    statusResponse = resp;
                    statusError = err;
                });

                // On HTTP error: continue polling (transient network issue).
                if (statusResponse == null)
                {
                    if (!string.IsNullOrEmpty(statusError))
                    {
                        SDKLogger.LogWarning("Pay: status polling error: " + statusError);
                    }
                    continue;
                }

                if (!statusResponse.success)
                {
                    // Treat as transient unless it becomes persistent; continue polling.
                    if (!string.IsNullOrEmpty(statusResponse.error))
                    {
                        SDKLogger.LogWarning("Pay: status polling unsuccessful: " + statusResponse.error);
                    }
                    continue;
                }

                if (statusResponse.status == "completed")
                {
                    onComplete?.Invoke(new PayResult
                    {
                        IsSuccess = true,
                        Status = "completed",
                        TxSignature = statusResponse.txSignature
                    });
                    yield break;
                }

                if (statusResponse.status == "execution_failed")
                {
                    if (statusResponse.canRetry && retryCount < MAX_RETRY_ATTEMPTS)
                    {
                        retryCount++;
                        SDKLogger.Log("Pay: execution_failed, retrying (" + retryCount + "/" + MAX_RETRY_ATTEMPTS + ")");

                        yield return new WaitForSeconds(RETRY_DELAY_SEC);

                        PayResponse retryPayResponse = null;
                        string retryPayError = null;

                        yield return _apiClient.PostPay(request, (resp, err) =>
                        {
                            retryPayResponse = resp;
                            retryPayError = err;
                        });

                        if (retryPayResponse == null)
                        {
                            // Treat as transient; continue polling existing intent.
                            if (!string.IsNullOrEmpty(retryPayError))
                            {
                                SDKLogger.LogWarning("Pay: retry call failed: " + retryPayError);
                            }
                            continue;
                        }

                        if (!retryPayResponse.success || retryPayResponse.status == "error")
                        {
                            string reason = retryPayResponse.message ??
                                            retryPayError ??
                                            "Execution failed";

                            onComplete?.Invoke(new PayResult
                            {
                                IsSuccess = false,
                                Status = "execution_failed",
                                ErrorMessage = reason
                            });
                            yield break;
                        }

                        if (retryPayResponse.status == "completed")
                        {
                            onComplete?.Invoke(new PayResult
                            {
                                IsSuccess = true,
                                Status = "completed",
                                TxSignature = retryPayResponse.txSignature
                            });
                            yield break;
                        }

                        if (retryPayResponse.status == "awaiting_topup" && !string.IsNullOrEmpty(retryPayResponse.intentId))
                        {
                            currentIntentId = retryPayResponse.intentId;
                            continue;
                        }

                        // Any other status from retry: fall through to polling; do not open Transak again here.
                        continue;
                    }
                    else
                    {
                        string reason = statusResponse.failureReason ?? statusResponse.error ?? "Execution failed";
                        onComplete?.Invoke(new PayResult
                        {
                            IsSuccess = false,
                            Status = "execution_failed",
                            ErrorMessage = reason
                        });
                        yield break;
                    }
                }

                if (statusResponse.status == "topup_failed")
                {
                    onComplete?.Invoke(new PayResult
                    {
                        IsSuccess = false,
                        Status = "topup_failed",
                        ErrorMessage = "Top-up failed. No funds were moved."
                    });
                    yield break;
                }

                if (statusResponse.status == "expired")
                {
                    onComplete?.Invoke(new PayResult
                    {
                        IsSuccess = false,
                        Status = "expired",
                        ErrorMessage = "Payment session expired."
                    });
                    yield break;
                }

                if (statusResponse.status == "awaiting_topup" ||
                    statusResponse.status == "topup_completed" ||
                    statusResponse.status == "executing" ||
                    statusResponse.status == "confirming")
                {
                    continue;
                }

                // Unknown status — continue polling, don't fail.
            }

            onComplete?.Invoke(new PayResult
            {
                IsSuccess = false,
                Status = "timeout",
                ErrorMessage = "Payment timed out after 5 minutes."
            });
        }
    }
}

