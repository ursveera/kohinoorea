export function observeSentinel(dotNetRef, sentinelOrId, callbackMethod) {
  if (!dotNetRef) throw new Error("dotNetRef required");
  const method = String(callbackMethod || "").trim();
  if (!method) throw new Error("callbackMethod required");

  const sentinelElement =
    typeof sentinelOrId === "string" ? document.getElementById(sentinelOrId) : sentinelOrId;
  if (!sentinelElement || !(sentinelElement instanceof Element)) {
    // Sentinel not in the DOM yet (or invalid id). Don't throw, just no-op.
    return null;
  }

  const observer = new IntersectionObserver(
    (entries) => {
      for (const e of entries) {
        if (e.isIntersecting) {
          dotNetRef.invokeMethodAsync(method).catch(() => {});
        }
      }
    },
    { root: null, rootMargin: "250px 0px 250px 0px", threshold: 0.01 }
  );

  observer.observe(sentinelElement);

  return {
    disconnect: () => observer.disconnect(),
  };
}
