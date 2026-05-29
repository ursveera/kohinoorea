import { initializeApp, getApps } from "https://www.gstatic.com/firebasejs/12.14.0/firebase-app.js";
import { getAuth, signInWithCustomToken, onAuthStateChanged } from "https://www.gstatic.com/firebasejs/12.14.0/firebase-auth.js";

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
  return getAuth();
}

export async function signIn(token) {
  const auth = ensureApp();
  const trimmed = String(token || "").trim();
  if (!trimmed) throw new Error("Missing custom token");
  try {
    const result = await signInWithCustomToken(auth, trimmed);
    console.log("[firebaseAuth] signed in", { uid: result.user?.uid || "" });
    return { uid: result.user?.uid || "" };
  } catch (e) {
    console.error("[firebaseAuth] signInWithCustomToken failed", e);
    throw e;
  }
}

export async function isSignedIn() {
  const auth = ensureApp();
  const user = auth.currentUser;
  return !!user;
}

export function onStateChanged(dotNetRef) {
  const auth = ensureApp();
  return onAuthStateChanged(auth, (user) => {
    dotNetRef.invokeMethodAsync("OnFirebaseAuthState", { uid: user?.uid || "", isSignedIn: !!user });
  });
}
