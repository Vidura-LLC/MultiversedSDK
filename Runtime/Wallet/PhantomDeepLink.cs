// File: Runtime/Wallet/PhantomDeepLink.cs
using System;
using System.Text;
using UnityEngine;

namespace Multiversed.Wallet
{
    /// <summary>
    /// Phantom wallet deep link handler with NaCl box encryption
    /// </summary>
    public static class PhantomDeepLink
    {
        private static string _appScheme;
        private static string _cluster = "devnet";
        private static string _appUrl = "https://multiversed.io";
        
        private static byte[] _secretKey;
        private static byte[] _publicKey;
        private static string _publicKeyBase58;
        private static byte[] _boxKey; // Pre-computed box key for current Phantom session

        public static void Initialize(string appScheme, bool isDevnet = true)
        {
            _appScheme = appScheme;
            _cluster = isDevnet ? "devnet" : "mainnet-beta";
            GenerateKeyPair();
            Debug.Log("[Phantom] Init: scheme=" + _appScheme);
        }

        private static void GenerateKeyPair()
        {
            _secretKey = new byte[32];
            using (var rng = System.Security.Cryptography.RandomNumberGenerator.Create())
                rng.GetBytes(_secretKey);
            _publicKey = ScalarMultBase(_secretKey);
            _publicKeyBase58 = Base58Encode(_publicKey);
            Debug.Log("[Phantom] Public key: " + _publicKeyBase58);
        }

public static string GetConnectUrl()
{
    if (string.IsNullOrEmpty(_appScheme)) 
    {
        Debug.LogError("[Phantom] App scheme not initialized");
        return null;
    }
    
    string redirectLink = _appScheme + "://onConnect";
    
    Debug.Log("[Phantom] Building connect URL:");
    Debug.Log("[Phantom]   App URL: " + _appUrl);
    Debug.Log("[Phantom]   Public Key: " + _publicKeyBase58);
    Debug.Log("[Phantom]   Redirect: " + redirectLink);
    Debug.Log("[Phantom]   Cluster: " + _cluster);
    
    // Use HTTPS universal link format (more reliable than phantom://)
    string url = "https://phantom.app/ul/v1/connect" +
           "?app_url=" + Uri.EscapeDataString(_appUrl) +
           "&dapp_encryption_public_key=" + _publicKeyBase58 +
           "&redirect_link=" + Uri.EscapeDataString(redirectLink) +
           "&cluster=" + _cluster;
    
    Debug.Log("[Phantom] Connect URL: " + url);
    
    return url;
}

        public static string GetSignAndSendTransactionUrl(string base64Transaction, string session)
        {
            if (string.IsNullOrEmpty(_appScheme) || _boxKey == null) 
            {
                Debug.LogError("[Phantom] Not initialized or not connected");
                return null;
            }

            // Convert base64 transaction to base58 (Phantom expects base58)
            byte[] txBytes = Convert.FromBase64String(base64Transaction);
            string txBase58 = Base58Encode(txBytes);

            // Payload must contain transaction (base58) and session
            string payloadJson = "{\"transaction\":\"" + txBase58 + "\",\"session\":\"" + session + "\"}";
            byte[] payloadBytes = Encoding.UTF8.GetBytes(payloadJson);

            Debug.Log("[Phantom] Payload length: " + payloadJson.Length);

            // Generate a random nonce
            byte[] nonce = new byte[24];
            using (var rng = System.Security.Cryptography.RandomNumberGenerator.Create())
                rng.GetBytes(nonce);

            // Encrypt the payload using NaCl secretbox
            byte[] encrypted = SecretBoxSeal(payloadBytes, nonce, _boxKey);
            
            if (encrypted == null)
            {
                Debug.LogError("[Phantom] Failed to encrypt transaction payload");
                return null;
            }

            string nonceB58 = Base58Encode(nonce);
            string payloadB58 = Base58Encode(encrypted);

            Debug.Log("[Phantom] Transaction encrypted, nonce: " + nonceB58);

            // Use HTTPS universal link format (more reliable than phantom://)
            return "https://phantom.app/ul/v1/signAndSendTransaction" +
                   "?dapp_encryption_public_key=" + _publicKeyBase58 +
                   "&nonce=" + nonceB58 +
                   "&redirect_link=" + Uri.EscapeDataString(_appScheme + "://onSignAndSendTransaction") +
                   "&payload=" + payloadB58;
        }

        /// <summary>
        /// Alternative: Use signTransaction instead of signAndSendTransaction
        /// </summary>
        public static string GetSignTransactionUrl(string base64Transaction, string session)
        {
            if (string.IsNullOrEmpty(_appScheme) || _boxKey == null) 
            {
                Debug.LogError("[Phantom] Not initialized or not connected");
                return null;
            }

            // Convert base64 transaction to base58
            byte[] txBytes = Convert.FromBase64String(base64Transaction);
            string txBase58 = Base58Encode(txBytes);

            // Payload for signTransaction
            string payloadJson = "{\"transaction\":\"" + txBase58 + "\",\"session\":\"" + session + "\"}";
            byte[] payloadBytes = Encoding.UTF8.GetBytes(payloadJson);

            // Generate nonce
            byte[] nonce = new byte[24];
            using (var rng = System.Security.Cryptography.RandomNumberGenerator.Create())
                rng.GetBytes(nonce);

            // Encrypt
            byte[] encrypted = SecretBoxSeal(payloadBytes, nonce, _boxKey);
            if (encrypted == null) return null;

            string nonceB58 = Base58Encode(nonce);
            string payloadB58 = Base58Encode(encrypted);

            Debug.Log("[Phantom] SignTransaction encrypted, nonce: " + nonceB58);

            // Use HTTPS universal link format
            return "https://phantom.app/ul/v1/signTransaction" +
                   "?dapp_encryption_public_key=" + _publicKeyBase58 +
                   "&nonce=" + nonceB58 +
                   "&redirect_link=" + Uri.EscapeDataString(_appScheme + "://onSignTransaction") +
                   "&payload=" + payloadB58;
        }

public static string GetDisconnectUrl(string session)
{
    if (string.IsNullOrEmpty(_appScheme)) return null;
    
    // Use HTTPS universal link format
    return "https://phantom.app/ul/v1/disconnect" +
           "?dapp_encryption_public_key=" + _publicKeyBase58 +
           "&redirect_link=" + Uri.EscapeDataString(_appScheme + "://onDisconnect");
}

        public static bool ParseConnectResponse(string url, out string publicKey, out string session, out string error)
        {
            publicKey = null;
            session = null;
            error = null;

            try
            {
                string errorCode = GetParam(url, "errorCode");
                if (!string.IsNullOrEmpty(errorCode))
                {
                    error = GetParam(url, "errorMessage") ?? "Error " + errorCode;
                    return false;
                }

                byte[] phantomPk = Base58Decode(GetParam(url, "phantom_encryption_public_key"));
                byte[] nonce = Base58Decode(GetParam(url, "nonce"));
                byte[] ciphertext = Base58Decode(GetParam(url, "data"));

                if (phantomPk == null || phantomPk.Length != 32 ||
                    nonce == null || nonce.Length != 24 ||
                    ciphertext == null || ciphertext.Length < 16)
                {
                    error = "Invalid parameters";
                    return false;
                }

                Debug.Log("[Phantom] Ciphertext len: " + ciphertext.Length + ", nonce len: " + nonce.Length);

                // Step 1: Compute raw shared secret via X25519
                byte[] sharedSecret = ScalarMult(_secretKey, phantomPk);
                Debug.Log("[Phantom] Raw shared[0-3]: " + sharedSecret[0] + "," + sharedSecret[1] + "," + sharedSecret[2] + "," + sharedSecret[3]);

                // Step 2: Derive box key using HSalsa20 with ZERO nonce (crypto_box_beforenm)
                byte[] zeroNonce = new byte[16];
                _boxKey = HSalsa20(sharedSecret, zeroNonce);
                Debug.Log("[Phantom] Box key[0-3]: " + _boxKey[0] + "," + _boxKey[1] + "," + _boxKey[2] + "," + _boxKey[3]);

                // Step 3: Decrypt using crypto_secretbox_open with the message nonce
                byte[] plaintext = SecretBoxOpen(ciphertext, nonce, _boxKey);
                
                if (plaintext != null)
                {
                    string json = Encoding.UTF8.GetString(plaintext);
                    Debug.Log("[Phantom] Decrypted: " + json);
                    
                    publicKey = ExtractJsonString(json, "public_key");
                    session = ExtractJsonString(json, "session");

                    if (!string.IsNullOrEmpty(publicKey) && publicKey.Length >= 32)
                    {
                        Debug.Log("[Phantom] SUCCESS! Wallet: " + publicKey);
                        return true;
                    }
                }

                error = "Decryption failed";
                return false;
            }
            catch (Exception e)
            {
                error = e.Message;
                Debug.LogError("[Phantom] Exception: " + e);
                return false;
            }
        }

        public static bool ParseSignatureResponse(string url, out string signature, out string error)
        {
            signature = null;
            error = null;

            string errorCode = GetParam(url, "errorCode");
            if (!string.IsNullOrEmpty(errorCode))
            {
                error = GetParam(url, "errorMessage") ?? "Error " + errorCode;
                return false;
            }

            // Encrypted response path (signTransaction / signAndSendTransaction)
            byte[] nonce = Base58Decode(GetParam(url, "nonce"));
            byte[] ciphertext = Base58Decode(GetParam(url, "data"));

            if (nonce != null && ciphertext != null && _boxKey != null)
            {
                byte[] plaintext = SecretBoxOpen(ciphertext, nonce, _boxKey);
                if (plaintext != null)
                {
                    string json = Encoding.UTF8.GetString(plaintext);
                    Debug.Log("[Phantom] Decrypted sign payload: " + json);

                    // Primary: Phantom may return `signature`
                    signature = ExtractJsonString(json, "signature");

                    // Fallback: some variants return `signed_transaction`
                    if (string.IsNullOrEmpty(signature))
                    {
                        signature = ExtractJsonString(json, "signed_transaction");
                    }

                    // Fallback: Phantom mobile returns `transaction` (base58 signed transaction)
                    if (string.IsNullOrEmpty(signature))
                    {
                        signature = ExtractJsonString(json, "transaction");
                        if (!string.IsNullOrEmpty(signature))
                        {
                            Debug.Log("[Phantom] Extracted transaction length: " + signature.Length);
                            // Log first and last 20 chars to verify extraction
                            int previewLen = Math.Min(20, signature.Length);
                            string preview = signature.Length > 40 
                                ? signature.Substring(0, previewLen) + "..." + signature.Substring(signature.Length - previewLen)
                                : signature;
                            Debug.Log("[Phantom] Transaction preview: " + preview);
                        }
                    }
                }
            }

            // Plain response path (older Phantom / dev tools)
            if (string.IsNullOrEmpty(signature))
                signature = GetParam(url, "signature");

            return !string.IsNullOrEmpty(signature);
        }

        public static void OpenUrl(string url) => Application.OpenURL(url);

        #region crypto_secretbox

        /// <summary>
        /// NaCl crypto_secretbox (XSalsa20-Poly1305) - SEAL/ENCRYPT
        /// Returns: [16-byte MAC][encrypted data]
        /// </summary>
        private static byte[] SecretBoxSeal(byte[] m, byte[] n, byte[] k)
        {
            // XSalsa20: first apply HSalsa20 to get subkey, then Salsa20
            byte[] n16 = new byte[16];
            Array.Copy(n, 0, n16, 0, 16);
            byte[] subkey = HSalsa20(k, n16);

            byte[] n8 = new byte[8];
            Array.Copy(n, 16, n8, 0, 8);

            // Generate keystream (need 32 + message length)
            int streamLen = 32 + m.Length;
            byte[] stream = Salsa20(n8, subkey, streamLen);

            // Encrypt: XOR message with stream[32:]
            byte[] ciphertext = new byte[m.Length];
            for (int i = 0; i < m.Length; i++)
                ciphertext[i] = (byte)(m[i] ^ stream[i + 32]);

            // First 32 bytes of stream are Poly1305 key
            byte[] polyKey = new byte[32];
            Array.Copy(stream, 0, polyKey, 0, 32);

            // Compute MAC over ciphertext
            byte[] mac = Poly1305(ciphertext, 0, ciphertext.Length, polyKey);

            // Return [MAC][ciphertext]
            byte[] result = new byte[16 + ciphertext.Length];
            Array.Copy(mac, 0, result, 0, 16);
            Array.Copy(ciphertext, 0, result, 16, ciphertext.Length);

            return result;
        }

        #endregion

        #region crypto_secretbox_open

        /// <summary>
        /// NaCl crypto_secretbox_open (XSalsa20-Poly1305)
        /// Ciphertext format: [16-byte MAC][encrypted data]
        /// </summary>
        private static byte[] SecretBoxOpen(byte[] c, byte[] n, byte[] k)
        {
            if (c.Length < 16) return null;

            // XSalsa20: first apply HSalsa20 to get subkey, then Salsa20
            byte[] n16 = new byte[16];
            Array.Copy(n, 0, n16, 0, 16);
            byte[] subkey = HSalsa20(k, n16);

            byte[] n8 = new byte[8];
            Array.Copy(n, 16, n8, 0, 8);

            // Generate keystream (need 32 + message length)
            int streamLen = 32 + c.Length;
            byte[] stream = Salsa20(n8, subkey, streamLen);

            // First 32 bytes of stream are Poly1305 key
            byte[] polyKey = new byte[32];
            Array.Copy(stream, 0, polyKey, 0, 32);

            // Verify MAC over ciphertext[16:]
            byte[] computedMac = Poly1305(c, 16, c.Length - 16, polyKey);

            bool valid = true;
            for (int i = 0; i < 16; i++)
                if (c[i] != computedMac[i]) valid = false;

            if (valid)
            {
                Debug.Log("[Phantom] MAC verified!");
                byte[] m = new byte[c.Length - 16];
                for (int i = 0; i < m.Length; i++)
                    m[i] = (byte)(c[i + 16] ^ stream[i + 32]);
                return m;
            }

            Debug.Log("[Phantom] MAC failed, trying alternatives...");

            // Debug: show what standard decryption produces
            byte[] debug = new byte[Math.Min(c.Length - 16, 100)];
            for (int i = 0; i < debug.Length; i++)
                debug[i] = (byte)(c[i + 16] ^ stream[i + 32]);
            
            try
            {
                string dbg = Encoding.UTF8.GetString(debug);
                Debug.Log("[Phantom] Decrypt attempt: " + dbg);
                if (dbg.Contains("public_key"))
                {
                    byte[] m = new byte[c.Length - 16];
                    for (int i = 0; i < m.Length; i++)
                        m[i] = (byte)(c[i + 16] ^ stream[i + 32]);
                    return m;
                }
            }
            catch { }

            // Try without the 16-byte MAC offset (maybe Phantom sends raw encrypted data)
            byte[] debug2 = new byte[Math.Min(c.Length, 100)];
            for (int i = 0; i < debug2.Length; i++)
                debug2[i] = (byte)(c[i] ^ stream[i + 32]);
            
            try
            {
                string dbg2 = Encoding.UTF8.GetString(debug2);
                Debug.Log("[Phantom] Decrypt attempt (no MAC): " + dbg2);
                if (dbg2.Contains("public_key"))
                {
                    byte[] m = new byte[c.Length];
                    for (int i = 0; i < m.Length; i++)
                        m[i] = (byte)(c[i] ^ stream[i + 32]);
                    return m;
                }
            }
            catch { }

            return null;
        }

        #endregion

        #region Crypto Primitives

        private static byte[] HSalsa20(byte[] k, byte[] n)
        {
            uint[] x = new uint[16];
            x[0] = 0x61707865; x[5] = 0x3320646e; x[10] = 0x79622d32; x[15] = 0x6b206574;
            x[1] = LD(k, 0); x[2] = LD(k, 4); x[3] = LD(k, 8); x[4] = LD(k, 12);
            x[11] = LD(k, 16); x[12] = LD(k, 20); x[13] = LD(k, 24); x[14] = LD(k, 28);
            x[6] = LD(n, 0); x[7] = LD(n, 4); x[8] = LD(n, 8); x[9] = LD(n, 12);

            for (int i = 0; i < 20; i += 2)
            {
                QR(ref x[0], ref x[4], ref x[8], ref x[12]);
                QR(ref x[5], ref x[9], ref x[13], ref x[1]);
                QR(ref x[10], ref x[14], ref x[2], ref x[6]);
                QR(ref x[15], ref x[3], ref x[7], ref x[11]);
                QR(ref x[0], ref x[1], ref x[2], ref x[3]);
                QR(ref x[5], ref x[6], ref x[7], ref x[4]);
                QR(ref x[10], ref x[11], ref x[8], ref x[9]);
                QR(ref x[15], ref x[12], ref x[13], ref x[14]);
            }

            byte[] o = new byte[32];
            ST(o, 0, x[0]); ST(o, 4, x[5]); ST(o, 8, x[10]); ST(o, 12, x[15]);
            ST(o, 16, x[6]); ST(o, 20, x[7]); ST(o, 24, x[8]); ST(o, 28, x[9]);
            return o;
        }

        private static byte[] Salsa20(byte[] n, byte[] k, int len)
        {
            byte[] o = new byte[len];
            uint[] s = new uint[16], x = new uint[16];
            
            s[0] = 0x61707865; s[5] = 0x3320646e; s[10] = 0x79622d32; s[15] = 0x6b206574;
            s[1] = LD(k, 0); s[2] = LD(k, 4); s[3] = LD(k, 8); s[4] = LD(k, 12);
            s[11] = LD(k, 16); s[12] = LD(k, 20); s[13] = LD(k, 24); s[14] = LD(k, 28);
            s[6] = LD(n, 0); s[7] = LD(n, 4);
            s[8] = 0; s[9] = 0;

            int pos = 0;
            while (pos < len)
            {
                for (int i = 0; i < 16; i++) x[i] = s[i];
                
                for (int i = 0; i < 20; i += 2)
                {
                    QR(ref x[0], ref x[4], ref x[8], ref x[12]);
                    QR(ref x[5], ref x[9], ref x[13], ref x[1]);
                    QR(ref x[10], ref x[14], ref x[2], ref x[6]);
                    QR(ref x[15], ref x[3], ref x[7], ref x[11]);
                    QR(ref x[0], ref x[1], ref x[2], ref x[3]);
                    QR(ref x[5], ref x[6], ref x[7], ref x[4]);
                    QR(ref x[10], ref x[11], ref x[8], ref x[9]);
                    QR(ref x[15], ref x[12], ref x[13], ref x[14]);
                }

                for (int i = 0; i < 16; i++) x[i] += s[i];

                for (int i = 0; i < 16 && pos < len; i++)
                {
                    o[pos++] = (byte)x[i];
                    if (pos < len) o[pos++] = (byte)(x[i] >> 8);
                    if (pos < len) o[pos++] = (byte)(x[i] >> 16);
                    if (pos < len) o[pos++] = (byte)(x[i] >> 24);
                }

                s[8]++;
                if (s[8] == 0) s[9]++;
            }
            return o;
        }

        private static void QR(ref uint a, ref uint b, ref uint c, ref uint d)
        {
            b ^= ROL(a + d, 7);
            c ^= ROL(b + a, 9);
            d ^= ROL(c + b, 13);
            a ^= ROL(d + c, 18);
        }

        private static byte[] Poly1305(byte[] m, int off, int len, byte[] key)
        {
            uint r0 = LD(key, 0) & 0x3FFFFFF;
            uint r1 = (LD(key, 3) >> 2) & 0x3FFFF03;
            uint r2 = (LD(key, 6) >> 4) & 0x3FFC0FF;
            uint r3 = (LD(key, 9) >> 6) & 0x3F03FFF;
            uint r4 = (LD(key, 12) >> 8) & 0x00FFFFF;

            uint h0 = 0, h1 = 0, h2 = 0, h3 = 0, h4 = 0;

            while (len > 0)
            {
                int n = Math.Min(16, len);
                uint c0 = 0, c1 = 0, c2 = 0, c3 = 0, c4 = 0;

                for (int i = 0; i < n; i++)
                {
                    uint b = m[off + i];
                    switch (i >> 2) {
                        case 0: c0 |= b << ((i & 3) * 8); break;
                        case 1: c1 |= b << ((i & 3) * 8); break;
                        case 2: c2 |= b << ((i & 3) * 8); break;
                        case 3: c3 |= b << ((i & 3) * 8); break;
                    }
                }
                if (n == 16) c4 = 1;
                else { int w = n >> 2; uint bit = 1u << ((n & 3) * 8);
                    switch (w) { case 0: c0 |= bit; break; case 1: c1 |= bit; break;
                                 case 2: c2 |= bit; break; case 3: c3 |= bit; break; } }

                h0 += c0 & 0x3FFFFFF;
                h1 += ((c0 >> 26) | (c1 << 6)) & 0x3FFFFFF;
                h2 += ((c1 >> 20) | (c2 << 12)) & 0x3FFFFFF;
                h3 += ((c2 >> 14) | (c3 << 18)) & 0x3FFFFFF;
                h4 += (c3 >> 8) | (c4 << 24);

                ulong t0 = (ulong)h0*r0 + (ulong)h1*(5*r4) + (ulong)h2*(5*r3) + (ulong)h3*(5*r2) + (ulong)h4*(5*r1);
                ulong t1 = (ulong)h0*r1 + (ulong)h1*r0 + (ulong)h2*(5*r4) + (ulong)h3*(5*r3) + (ulong)h4*(5*r2);
                ulong t2 = (ulong)h0*r2 + (ulong)h1*r1 + (ulong)h2*r0 + (ulong)h3*(5*r4) + (ulong)h4*(5*r3);
                ulong t3 = (ulong)h0*r3 + (ulong)h1*r2 + (ulong)h2*r1 + (ulong)h3*r0 + (ulong)h4*(5*r4);
                ulong t4 = (ulong)h0*r4 + (ulong)h1*r3 + (ulong)h2*r2 + (ulong)h3*r1 + (ulong)h4*r0;

                ulong cc = t0 >> 26; t0 &= 0x3FFFFFF; t1 += cc;
                cc = t1 >> 26; t1 &= 0x3FFFFFF; t2 += cc;
                cc = t2 >> 26; t2 &= 0x3FFFFFF; t3 += cc;
                cc = t3 >> 26; t3 &= 0x3FFFFFF; t4 += cc;
                cc = t4 >> 26; t4 &= 0x3FFFFFF; t0 += cc * 5;
                cc = t0 >> 26; t0 &= 0x3FFFFFF; t1 += cc;

                h0 = (uint)t0; h1 = (uint)t1; h2 = (uint)t2; h3 = (uint)t3; h4 = (uint)t4;
                off += 16; len -= 16;
            }

            uint g0 = h0 + 5, g1 = h1 + (g0 >> 26); g0 &= 0x3FFFFFF;
            uint g2 = h2 + (g1 >> 26); g1 &= 0x3FFFFFF;
            uint g3 = h3 + (g2 >> 26); g2 &= 0x3FFFFFF;
            uint g4 = h4 + (g3 >> 26) - (1 << 26); g3 &= 0x3FFFFFF;

            uint mask = (g4 >> 31) - 1;
            g0 &= mask; g1 &= mask; g2 &= mask; g3 &= mask; g4 &= mask;
            mask = ~mask;
            h0 = (h0 & mask) | g0; h1 = (h1 & mask) | g1;
            h2 = (h2 & mask) | g2; h3 = (h3 & mask) | g3;
            h4 = (h4 & mask) | g4;

            h0 |= h1 << 26; h1 = (h1 >> 6) | (h2 << 20);
            h2 = (h2 >> 12) | (h3 << 14); h3 = (h3 >> 18) | (h4 << 8);

            ulong f = (ulong)h0 + LD(key, 16); h0 = (uint)f;
            f = (ulong)h1 + LD(key, 20) + (f >> 32); h1 = (uint)f;
            f = (ulong)h2 + LD(key, 24) + (f >> 32); h2 = (uint)f;
            f = (ulong)h3 + LD(key, 28) + (f >> 32); h3 = (uint)f;

            byte[] mac = new byte[16];
            ST(mac, 0, h0); ST(mac, 4, h1); ST(mac, 8, h2); ST(mac, 12, h3);
            return mac;
        }

        #endregion

        #region X25519

        private static byte[] ScalarMultBase(byte[] n)
        {
            byte[] p = new byte[32]; p[0] = 9;
            return ScalarMult(n, p);
        }

        private static byte[] ScalarMult(byte[] n, byte[] p)
        {
            byte[] z = (byte[])n.Clone();
            z[31] &= 127; z[31] |= 64; z[0] &= 248;

            long[] x = Unpack(p);
            long[] a = new long[16]; a[0] = 1;
            long[] b = (long[])x.Clone();
            long[] c = new long[16];
            long[] d = new long[16]; d[0] = 1;
            long[] e = new long[16];
            long[] f = new long[16];

            int swap = 0;
            for (int i = 254; i >= 0; i--)
            {
                int bit = (z[i >> 3] >> (i & 7)) & 1;
                swap ^= bit; Sel(a, b, swap); Sel(c, d, swap); swap = bit;
                FAdd(e, a, c); FSub(a, a, c); FAdd(c, b, d); FSub(b, b, d);
                FSq(d, e); FSq(f, a); FMul(a, c, a); FMul(c, b, e);
                FAdd(e, a, c); FSub(a, a, c); FSq(b, a); FSub(c, d, f);
                FMul121665(a, c); FAdd(a, a, d); FMul(c, c, a);
                FMul(a, d, f); FMul(d, b, x); FSq(b, e);
            }
            Sel(a, b, swap); Sel(c, d, swap);
            FInv(c, c); FMul(a, a, c);
            return Pack(a);
        }

        private static long[] Unpack(byte[] n)
        {
            long[] o = new long[16];
            for (int i = 0; i < 16; i++)
                o[i] = (n[2*i] & 0xFFL) | ((n[2*i+1] & 0xFFL) << 8);
            o[15] &= 0x7FFF;
            return o;
        }

        private static byte[] Pack(long[] n)
        {
            long[] t = (long[])n.Clone();
            FCar(t); FCar(t); FCar(t);
            long[] m = new long[16];
            for (int j = 0; j < 2; j++)
            {
                m[0] = t[0] - 0xFFED;
                for (int i = 1; i < 15; i++) { m[i] = t[i] - 0xFFFF - ((m[i-1] >> 16) & 1); m[i-1] &= 0xFFFF; }
                m[15] = t[15] - 0x7FFF - ((m[14] >> 16) & 1);
                long bv = (m[15] >> 16) & 1; m[14] &= 0xFFFF;
                Sel(t, m, (int)(1 - bv));
            }
            byte[] o = new byte[32];
            for (int i = 0; i < 16; i++) { o[2*i] = (byte)(t[i] & 0xFF); o[2*i+1] = (byte)((t[i] >> 8) & 0xFF); }
            return o;
        }

        private static void FCar(long[] o) {
            for (int i = 0; i < 16; i++) {
                o[i] += 65536; long cv = o[i] >> 16;
                o[(i+1)%16] += cv - 1 + (i == 15 ? 37*(cv-1) : 0);
                o[i] -= cv << 16;
            }
        }
        private static void Sel(long[] p, long[] q, int b) {
            long cv = ~(b - 1);
            for (int i = 0; i < 16; i++) { long t = cv & (p[i] ^ q[i]); p[i] ^= t; q[i] ^= t; }
        }
        private static void FAdd(long[] o, long[] a, long[] b) { for (int i = 0; i < 16; i++) o[i] = a[i] + b[i]; }
        private static void FSub(long[] o, long[] a, long[] b) { for (int i = 0; i < 16; i++) o[i] = a[i] - b[i]; }
        private static void FMul(long[] o, long[] a, long[] b) {
            long[] t = new long[31];
            for (int i = 0; i < 16; i++) for (int j = 0; j < 16; j++) t[i+j] += a[i] * b[j];
            for (int i = 0; i < 15; i++) t[i] += 38 * t[i+16];
            for (int i = 0; i < 16; i++) o[i] = t[i];
            FCar(o); FCar(o);
        }
        private static void FSq(long[] o, long[] a) { FMul(o, a, a); }
        private static void FMul121665(long[] o, long[] a) { for (int i = 0; i < 16; i++) o[i] = a[i] * 121665; FCar(o); }
        private static void FInv(long[] o, long[] a) {
            long[] cv = (long[])a.Clone();
            for (int i = 253; i >= 0; i--) { FSq(cv, cv); if (i != 2 && i != 4) FMul(cv, cv, a); }
            for (int i = 0; i < 16; i++) o[i] = cv[i];
        }

        #endregion

        #region Helpers

        private static uint LD(byte[] x, int i) => (uint)x[i] | ((uint)x[i+1] << 8) | ((uint)x[i+2] << 16) | ((uint)x[i+3] << 24);
        private static void ST(byte[] x, int i, uint v) { x[i] = (byte)v; x[i+1] = (byte)(v>>8); x[i+2] = (byte)(v>>16); x[i+3] = (byte)(v>>24); }
        private static uint ROL(uint x, int n) => (x << n) | (x >> (32 - n));

        private static string GetParam(string url, string key)
        {
            int q = url.IndexOf('?');
            if (q < 0) return null;
            foreach (var p in url.Substring(q + 1).Split('&'))
            {
                int eq = p.IndexOf('=');
                if (eq > 0 && p.Substring(0, eq) == key)
                    return Uri.UnescapeDataString(p.Substring(eq + 1));
            }
            return null;
        }

        private static string ExtractJsonString(string json, string key)
        {
            string pattern = "\"" + key + "\"";
            int idx = json.IndexOf(pattern);
            if (idx < 0) return null;
            int colon = json.IndexOf(':', idx);
            if (colon < 0) return null;
            int start = json.IndexOf('"', colon + 1);
            if (start < 0) return null;
            start++;
            int end = json.IndexOf('"', start);
            if (end < 0) return null;
            return json.Substring(start, end - start);
        }

        private const string B58 = "123456789ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz";

        private static string Base58Encode(byte[] data)
        {
            if (data == null) return null;
            int zeros = 0; while (zeros < data.Length && data[zeros] == 0) zeros++;
            var result = new System.Collections.Generic.List<char>();
            byte[] buf = (byte[])data.Clone();
            while (!AllZero(buf)) { int r = Div58(buf); result.Insert(0, B58[r]); }
            for (int i = 0; i < zeros; i++) result.Insert(0, '1');
            return new string(result.ToArray());
        }

        private static byte[] Base58Decode(string s)
        {
            if (string.IsNullOrEmpty(s)) return null;
            int ones = 0; while (ones < s.Length && s[ones] == '1') ones++;
            byte[] buf = new byte[s.Length];
            foreach (char ch in s) {
                int d = B58.IndexOf(ch); if (d < 0) return null;
                int carry = d;
                for (int j = buf.Length - 1; j >= 0; j--) { carry += 58 * buf[j]; buf[j] = (byte)(carry & 0xFF); carry >>= 8; }
            }
            int first = 0; while (first < buf.Length && buf[first] == 0) first++;
            byte[] result = new byte[ones + buf.Length - first];
            Array.Copy(buf, first, result, ones, buf.Length - first);
            return result;
        }

        private static bool AllZero(byte[] b) { foreach (byte x in b) if (x != 0) return false; return true; }
        private static int Div58(byte[] b) { int r = 0; for (int i = 0; i < b.Length; i++) { int v = (r << 8) + b[i]; b[i] = (byte)(v / 58); r = v % 58; } return r; }

        #endregion
    }
}