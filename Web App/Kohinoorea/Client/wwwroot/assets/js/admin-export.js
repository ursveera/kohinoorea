export function downloadTextFile(filename, content, mimeType) {
  const safeName = String(filename || "export.txt");
  const mime = String(mimeType || "text/plain;charset=utf-8");
  const blob = new Blob([content ?? ""], { type: mime });
  const url = URL.createObjectURL(blob);

  const a = document.createElement("a");
  a.href = url;
  a.download = safeName;
  a.style.display = "none";
  document.body.appendChild(a);
  a.click();
  document.body.removeChild(a);

  setTimeout(() => URL.revokeObjectURL(url), 2500);
}

export function downloadBytesFile(filename, bytes, mimeType) {
  const safeName = String(filename || "export.bin");
  const mime = String(mimeType || "application/octet-stream");
  const blob = new Blob([bytes], { type: mime });
  const url = URL.createObjectURL(blob);

  const a = document.createElement("a");
  a.href = url;
  a.download = safeName;
  a.style.display = "none";
  document.body.appendChild(a);
  a.click();
  document.body.removeChild(a);

  setTimeout(() => URL.revokeObjectURL(url), 2500);
}

export function openHtmlPrintWindow(htmlContent, title) {
  const w = window.open("", "_blank", "noopener,noreferrer");
  if (!w) return false;

  w.document.open();
  w.document.write(htmlContent ?? "");
  w.document.close();
  w.document.title = String(title || "Export");

  const triggerPrint = () => {
    try {
      w.focus();
      w.print();
    } catch {
      // ignore
    }
  };

  // Some browsers need a load event for styles/layout.
  try {
    w.addEventListener("load", () => setTimeout(triggerPrint, 400), { once: true });
  } catch {
    // ignore
  }

  // Fallback if load doesn't fire (document.write flows).
  setTimeout(triggerPrint, 900);

  return true;
}
