export async function assertFreeAccount(fetcher, accountId, token) {
  if (!token || !/^[a-f0-9]{32}$/i.test(accountId ?? "")) throw new Error("Cloudflare-Zugangsdaten fehlen oder Konto-ID ist ungültig.");
  async function read(path) {
    const response = await fetcher(`https://api.cloudflare.com/client/v4/accounts/${accountId}/${path}`, { headers: { Authorization: `Bearer ${token}` } });
    let data;
    try { data = await response.json(); } catch { throw new Error("Cloudflare-Tarif konnte nicht geprüft werden. Keine Bereitstellung."); }
    if (!response.ok || data?.success !== true) throw new Error("Cloudflare-Tarif konnte nicht geprüft werden. Keine Bereitstellung.");
    return data;
  }
  // Workers Free is the default in the absence of a Workers subscription.
  // /workers/standard reports the usage model ({ standard: true }), not the plan.
  // Check the complete billing subscription list, including legacy Workers plans.
  const subscriptions = await read("subscriptions");
  if (!Array.isArray(subscriptions.result) || (subscriptions.result_info?.total_pages ?? 1) > 1) throw new Error("Abonnements nicht vollständig prüfbar. Keine Bereitstellung.");
  for (const subscription of subscriptions.result) {
    if (!subscription || typeof subscription !== "object" || !subscription.rate_plan || typeof subscription.rate_plan !== "object") throw new Error("Abonnement nicht eindeutig prüfbar. Keine Bereitstellung.");
    if (["canceled", "cancelled", "expired"].includes(subscription.state ?? subscription.status)) continue;
    const plan = subscription.rate_plan ?? {};
    const description = JSON.stringify({ plan, product: subscription.product ?? {} }).toLowerCase();
    if (description.includes("workers") && !(description.includes("free") && (subscription.price ?? plan.price) === 0)) throw new Error("Kostenpflichtiges oder unklares Workers-Abonnement erkannt. Keine Bereitstellung und kein Tarifwechsel.");
  }
}
