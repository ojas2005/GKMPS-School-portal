#!/usr/bin/env bash
# One-time setup so GitHub Actions can deploy the backend (Backend CI → deploy job).
#
# Creates an Entra app "gkmps-github-deploy" that GitHub signs in as through OIDC: there is
# no password or key to store or leak, and only the backend repo's "production" environment
# can use it. Its only permission is Contributor on the gkmps-api container app. It costs
# nothing. Safe to re-run.
set -euo pipefail

REPO=ojas2005/GKMPS-School-portal
RG=gkmps-prod
APP_NAME=gkmps-api
NAME=gkmps-github-deploy

SUB=$(az account show --query id -o tsv)
TENANT=$(az account show --query tenantId -o tsv)
SCOPE=$(az containerapp show -g "$RG" -n "$APP_NAME" --query id -o tsv)

CLIENT_ID=$(az ad app list --display-name "$NAME" --query "[0].appId" -o tsv)
if [ -z "$CLIENT_ID" ]; then
  CLIENT_ID=$(az ad app create --display-name "$NAME" --query appId -o tsv)
fi
az ad sp show --id "$CLIENT_ID" >/dev/null 2>&1 || az ad sp create --id "$CLIENT_ID" >/dev/null

if ! az ad app federated-credential list --id "$CLIENT_ID" --query "[?name=='backend-main-production']" -o tsv | grep -q .; then
  az ad app federated-credential create --id "$CLIENT_ID" --parameters "{
    \"name\": \"backend-main-production\",
    \"issuer\": \"https://token.actions.githubusercontent.com\",
    \"subject\": \"repo:$REPO:environment:production\",
    \"audiences\": [\"api://AzureADTokenExchange\"]
  }" >/dev/null
fi

# A new service principal can take a few seconds to be visible to role assignments.
for i in 1 2 3 4 5 6; do
  az role assignment create --assignee "$CLIENT_ID" --role Contributor --scope "$SCOPE" >/dev/null 2>&1 && break
  sleep 10
done
az role assignment list --assignee "$CLIENT_ID" --scope "$SCOPE" --query "[].roleDefinitionName" -o tsv | grep -q Contributor

# These are identifiers, not secrets.
gh variable set AZURE_CLIENT_ID --body "$CLIENT_ID" -R "$REPO"
gh variable set AZURE_TENANT_ID --body "$TENANT" -R "$REPO"
gh variable set AZURE_SUBSCRIPTION_ID --body "$SUB" -R "$REPO"

echo "Done: GitHub can now deploy $APP_NAME (client id $CLIENT_ID)."
