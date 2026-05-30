import { initializeApp, getApps } from "https://www.gstatic.com/firebasejs/12.14.0/firebase-app.js";
import { getAuth } from "https://www.gstatic.com/firebasejs/12.14.0/firebase-auth.js";
import {
  getDatabase,
  ref,
  push,
  set,
  onChildAdded,
  off,
  onValue,
  serverTimestamp,
  query,
  limitToLast,
  update,
  get,
  orderByChild,
} from "https://www.gstatic.com/firebasejs/12.14.0/firebase-database.js";

const firebaseConfig = {
  apiKey: "AIzaSyBTuXjwnvahB0McPkMMwRqc1LHppvYUjoQ",
  authDomain: "kohinoorea-2e281.firebaseapp.com",
  databaseURL: "https://kohinoorea-2e281-default-rtdb.firebaseio.com",
  projectId: "kohinoorea-2e281",
  storageBucket: "kohinoorea-2e281.firebasestorage.app",
  messagingSenderId: "368109784531",
  appId: "1:368109784531:web:8e3588c43003c8dc69f326",
  measurementId: "G-MSEZ3G92DM",
};

function ensureApp() {
  if (!getApps().length) {
    initializeApp(firebaseConfig);
  }
  return getDatabase();
}

function roomPath(roomId) {
  const clean = String(roomId || "").trim();
  if (!clean) throw new Error("roomId is required");
  return `supportRooms/${clean}`;
}

function clientIdOrFallback(explicitClientId) {
  if (explicitClientId && String(explicitClientId).trim()) return String(explicitClientId).trim();
  if (globalThis.crypto?.randomUUID) return globalThis.crypto.randomUUID();
  return `${Date.now()}-${Math.random().toString(16).slice(2)}`;
}

export function createSupportChat(dotNetRef, roomId, options) {
  const db = ensureApp();
  const resolvedClientId = clientIdOrFallback(options?.clientId);
  const resolvedRole = String(options?.role || "user").toLowerCase() === "admin" ? "admin" : "user";
  const resolvedName = String(options?.displayName || (resolvedRole === "admin" ? "Admin" : "User")).slice(0, 80);

  try {
    const auth = getAuth();
    console.log("[supportChat] create", {
      roomId,
      resolvedRole,
      currentUserUid: auth.currentUser?.uid || null,
      isSignedIn: !!auth.currentUser,
    });
  } catch (e) {
    console.warn("[supportChat] auth check failed", e);
  }

  const base = roomPath(roomId);
  const messagesRef = ref(db, `${base}/messages`);
  const typingRef = ref(db, `${base}/typing`);
  const metaRef = ref(db, `${base}/meta`);

  const disposers = [];

  const messagesQuery = query(messagesRef, orderByChild("createdAt"), limitToLast(200));
  const onMsg = onChildAdded(messagesQuery, (snap) => {
    const val = snap.val() || {};
    dotNetRef.invokeMethodAsync("OnMessage", {
      id: snap.key,
      text: val.text || "",
      senderRole: val.senderRole || "user",
      senderName: val.senderName || "",
      clientId: val.clientId || "",
      createdAt: val.createdAt || null,
    });
  });
  disposers.push(() => off(messagesQuery, "child_added", onMsg));

  const onTyping = onValue(typingRef, (snap) => {
    const map = snap.val() || {};
    const now = Date.now();
    const active = [];

    for (const [key, value] of Object.entries(map)) {
      if (!value || typeof value !== "object") continue;
      if (key === resolvedClientId) continue;
      const isTyping = !!value.isTyping;
      const lastAt = value.lastAt ? Number(value.lastAt) : 0;
      if (!isTyping) continue;
      if (lastAt > 0 && now - lastAt > 10_000) continue;
      active.push({
        clientId: key,
        role: value.role || "user",
        displayName: value.displayName || "",
      });
    }

    dotNetRef.invokeMethodAsync("OnTyping", active);
  });
  disposers.push(() => off(typingRef, "value", onTyping));

  const onMeta = onValue(metaRef, (snap) => {
    const val = snap.val() || {};
    dotNetRef.invokeMethodAsync("OnMeta", {
      status: val.status || null,
      updatedAt: val.updatedAt || null,
      lastMessage: val.lastMessage || null,
    });
  });
  disposers.push(() => off(metaRef, "value", onMeta));

  // Ensure room meta exists (best-effort) without overwriting existing keys.
  update(metaRef, { updatedAt: serverTimestamp() }).catch(() => {});

  async function setTyping(isTyping) {
    const node = ref(db, `${base}/typing/${resolvedClientId}`);
    try {
      await set(node, {
        isTyping: !!isTyping,
        lastAt: Date.now(),
        role: resolvedRole,
        displayName: resolvedName,
      });
    } catch (e) {
      console.error("[supportChat] setTyping failed", { roomId, resolvedRole }, e);
      throw e;
    }
  }

  async function sendMessage(text) {
    const trimmed = String(text || "").trim();
    if (!trimmed) return;
    const newRef = push(messagesRef);
    try {
      await set(newRef, {
        text: trimmed,
        senderRole: resolvedRole,
        senderName: resolvedName,
        clientId: resolvedClientId,
        createdAt: Date.now(),
      });
      await update(metaRef, { updatedAt: serverTimestamp(), lastMessage: trimmed.slice(0, 160) }).catch(() => {});
    } catch (e) {
      console.error("[supportChat] sendMessage failed", { roomId, resolvedRole }, e);
      throw e;
    }
  }

  async function setStatus(status) {
    const trimmed = String(status || "").trim();
    if (!trimmed) return;
    await update(metaRef, { status: trimmed, updatedAt: serverTimestamp() });
  }

  return {
    roomId,
    getClientId: () => resolvedClientId,
    getRole: () => resolvedRole,
    getDisplayName: () => resolvedName,
    sendMessage,
    setStatus,
    setTyping,
    dispose: () => {
      for (const d of disposers.splice(0)) {
        try {
          d();
        } catch {}
      }
      // Best-effort clear typing
      setTyping(false).catch(() => {});
    },
  };
}

export async function listSupportRooms() {
  const db = ensureApp();
  const roomsRef = ref(db, "supportRooms");
  const snap = await get(roomsRef);
  const val = snap.val() || {};
  const rooms = [];

  for (const [roomId, room] of Object.entries(val)) {
    const meta = room?.meta || {};
    rooms.push({
      roomId,
      updatedAt: meta.updatedAt || null,
      lastMessage: meta.lastMessage || "",
    });
  }

  // Sort newest first (fallback when updatedAt isn't a number)
  rooms.sort((a, b) => {
    const av = typeof a.updatedAt === "number" ? a.updatedAt : 0;
    const bv = typeof b.updatedAt === "number" ? b.updatedAt : 0;
    return bv - av;
  });

  return rooms;
}
