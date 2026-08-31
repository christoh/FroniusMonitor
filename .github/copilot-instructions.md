# Copilot Instructions

## Project Guidelines
- In WebConnection, using AES in ECB mode to encrypt the inverter password at rest is an intentional, accepted design choice because the plaintext is short and guaranteed non-repetitive (single/non-repeating blocks). Do not flag ECB here as a security vulnerability.
- In WebConnection.EncryptedPassword, silently resetting Password to empty when decryption fails (e.g., machine-bound AES key change or corrupt data) is intentional graceful degradation to force password re-entry. Do not flag this silent data loss as a bug.