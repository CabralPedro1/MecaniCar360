"use strict";
const boton = document.getElementById("mostrarPassword");
const password = document.getElementById("password");
if (boton && password) boton.addEventListener("click", () => {
    const visible = password.type === "password";
    password.type = visible ? "text" : "password";
    boton.textContent = visible ? "Ocultar" : "Mostrar";
    boton.setAttribute("aria-pressed", String(visible));
});
