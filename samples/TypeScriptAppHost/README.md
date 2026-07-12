# TypeScript AppHost sample

This sample hosts the existing ASP.NET Core web sample from a TypeScript AppHost.

1. Run `npm install`.
2. Run `aspire restore --non-interactive`.
3. Set the `bunny-storage-zone-name` and secret `bunny-api-key` Aspire parameters for deployment.
4. Run locally with `aspire start --non-interactive --isolated`.
5. Deploy with `aspire deploy --non-interactive`.

Local execution uses the Azure Storage resource. Bunny provisioning occurs only during `aspire deploy`.
