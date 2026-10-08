export async function assertFreeAccount(fetcher, accountId, token) {
  if (!token || !/^[a-f0-9]{32}$/i.test(accountId ?? "")) throw new Error("Cloudflare-Zugangsdaten fehlen oder Konto-ID ist ungültig.");
  async function read(path) {
    const response = await fetcher(`https://api.cloudflare.com/client/v4/accounts/${accountId}/${path}`, { headers: { Authorization: `Bearer ${token}` } });
    let data;
    try { data = await response.json(); } catch { throw new Error("Cloudflare-Tarif konnte nicht geprüft werden. Keine Bereitstellung."); }
    if (!response.ok || !data.success) throw new Error("Cloudflare-Tarif konnte nicht geprüft werden. Keine Bereitstellung.");
    return data;
  }
  const standard = await read("workers/standard");
  if (standard.result?.enabled !== false) throw new Error("Workers Free nicht bestätigt. Keine Bereitstellung und kein Tarifwechsel.");
  // Also reject legacy paid subscriptions, which may predate Workers Standard.
  const subscriptions = await read("subscriptions");
  if (!Array.isArray(subscriptions.result) || (subscriptions.result_info?.total_pages ?? 1) > 1) throw new Error("Abonnements nicht vollständig prüfbar. Keine Bereitstellung.");
  for (const subscription of subscriptions.result) {
    if (["canceled", "cancelled", "expired"].includes(subscription.state ?? subscription.status)) continue;
    const plan = subscription.rate_plan ?? {};
    const description = JSON.stringify({ plan, product: subscription.product ?? {} }).toLowerCase();
    if (description.includes("workers") && !(description.includes("free") && plan.price === 0)) throw new Error("Kostenpflichtiges Workers-Abonnement erkannt. Keine Bereitstellung.");
  }
}
