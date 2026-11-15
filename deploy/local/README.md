# Local containers (docker compose)

Runs the local infrastructure and the services that are containerised so far.

| Service | URL on the host | Notes |
|---|---|---|
| `postgres` | `localhost:5432` | Local development credentials only (`postgres` / `postgres`) |
| `sso` | `https://localhost:5001` | `http://localhost:5000` only redirects to HTTPS |

## First-time setup: a local HTTPS certificate

The SSO is an OpenID Connect provider, and OIDC requires HTTPS. Browsers also refuse `Secure` cookies (used for the
login session) over plain HTTP. So even locally, the SSO container needs a certificate that your browser trusts.

### Which certificate, and why

| Option | How it works | When to use it |
|---|---|---|
| **.NET dev certificate** (`dotnet dev-certs`) *(used here)* | The .NET SDK creates a self-signed certificate for `localhost` and can add it to your trusted root store. `dotnet run` already uses it. | One certificate for `localhost`, no extra tools. Enough while every service is reached as `localhost:<port>`. |
| **mkcert** | Creates a small local certificate authority (CA), trusts it once, then issues certificates for any names you want (`localhost`, `sso`, `chattyduck.localhost`, …). | When containers have to call each other over HTTPS by service name, or when Envoy serves several host names. This will probably be needed once Envoy and the BFF are added (ADR 0003). |
| **Plain `openssl` self-signed certificate** | You generate the key and certificate yourself and trust them by hand. | Only to learn how certificates work: every step that the other two automate is manual. |

A *self-signed* certificate is one that nobody else vouches for, so a browser warns about it until you tell your
operating system to trust it. "Trusting" adds it to the trusted root store of **your user account on this machine only**.
That is safe for a certificate that exists only on your machine, and it is why the exported file must never be committed
(`certs/` is git-ignored) or reused anywhere else.

### Steps (Windows, macOS, Linux)

Run these from the repository root.

1. **Create and trust the .NET development certificate.** Skip this if `dotnet run` on the SSO already shows no
   certificate warning in your browser.

   ```bash
   dotnet dev-certs https --trust
   ```

   On Windows a security dialog asks you to confirm adding the certificate; on macOS you are asked for your password.
   On Linux, `--trust` only covers some distributions and browsers. See the
   [Microsoft docs](https://learn.microsoft.com/aspnet/core/security/enforcing-ssl#trust-the-aspnet-core-https-development-certificate)
   for the manual steps.

   Check the result:

   ```bash
   dotnet dev-certs https --check --trust
   ```

2. **Pick a password and export the certificate (with its private key) for the container.**
   The container cannot read your Windows or macOS certificate store, so it needs the certificate as a file.

   ```bash
   dotnet dev-certs https --export-path deploy/local/certs/sso.pfx --password "<your-password>"
   ```

   This exports the *same* certificate you trusted in step 1, which is why the browser will trust the container too.

3. **Tell compose the password.** Copy the example file and put the same password in it:

   ```bash
   cp deploy/local/.env.example deploy/local/.env
   ```

   Then edit `deploy/local/.env` and set `SSO_CERT_PASSWORD=<your-password>`. The `.env` file is git-ignored.

4. **Start the stack.** Stop a locally running SSO first, because both use port 5001.

   ```bash
   docker compose -f deploy/local/compose.yaml up -d --build
   ```

5. **Check it.**
   - <https://localhost:5001/.well-known/openid-configuration> opens without a certificate warning, and `issuer` is `https://localhost:5001`.
   - <http://localhost:5000> redirects to `https://localhost:5001`.

### How the pieces fit together

- `certs/sso.pfx` is mounted read-only into the container at `/https/sso.pfx`. It is never copied into the image, so
  the image contains no key material and can be shared.
- Kestrel loads it through the standard configuration keys `Kestrel__Certificates__Default__Path` and `__Password`.
  In Kubernetes or the cloud the same keys are fed from a secret, or TLS is terminated by the ingress / Envoy instead.
- `ASPNETCORE_HTTPS_PORT=5001` is the port the *browser* sees. The HTTPS redirect uses it, because the container itself
  listens on 8081.
- HSTS is enabled only outside Development, so your browser does not pin `localhost` to HTTPS for every other local app.

### Troubleshooting

- **`SSO_CERT_PASSWORD` is required:** `deploy/local/.env` is missing or empty (step 3).
- **The container exits with a certificate or password error:** the password in `.env` does not match the one used in step 2. Export again.
- **The browser still warns:** step 1 did not trust the certificate, or you exported a different one. Run
  `dotnet dev-certs https --clean`, then repeat steps 1 and 2.
- **The certificate expired:** the dev certificate is valid for one year. Repeat steps 1 and 2.
