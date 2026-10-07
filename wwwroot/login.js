const form = document.querySelector("#login-form");
const errorMessage = document.querySelector("#login-error");
const loginButton = document.querySelector("#login-button");
let csrfToken;

async function initializeLogin() {
  try {
    const tokenResponse = await fetch("/api/auth/csrf", { credentials: "same-origin" });
    if (!tokenResponse.ok) throw new Error("Unable to initialize secure sign-in. Reload the page and try again.");
    csrfToken = (await tokenResponse.json()).requestToken;

    const sessionResponse = await fetch("/api/auth/me", { credentials: "same-origin" });
    if (sessionResponse.ok) window.location.replace("/");
    else if (sessionResponse.status !== 401) throw new Error("Unable to check your session. Reload the page and try again.");
  } catch (error) {
    errorMessage.textContent = error.message;
    loginButton.disabled = true;
  }
}

form.addEventListener("submit", async event => {
  event.preventDefault();
  errorMessage.textContent = "";
  loginButton.disabled = true;

  const credentials = Object.fromEntries(new FormData(form).entries());
  try {
    const response = await fetch("/api/auth/login", {
      method: "POST",
      credentials: "same-origin",
      headers: {
        "Content-Type": "application/json",
        "X-CSRF-TOKEN": csrfToken
      },
      body: JSON.stringify(credentials)
    });

    if (response.status === 204) {
      window.location.replace("/");
      return;
    }

    if (response.status === 429) {
      errorMessage.textContent = "Too many sign-in attempts. Wait a minute and try again.";
    } else if (response.status === 401) {
      errorMessage.textContent = "The username or password is incorrect.";
    } else {
      errorMessage.textContent = "Sign-in failed. Reload the page and try again.";
    }
  } catch {
    errorMessage.textContent = "Unable to reach the server. Check your connection and try again.";
  } finally {
    loginButton.disabled = false;
  }
});

initializeLogin();
