/* ============================================
   UI ENHANCEMENTS LOGIC
   Toast, Scroll to Top, Page Loader
   ============================================ */

document.addEventListener("DOMContentLoaded", () => {
  initScrollToTop();
  initPageLoader();

  // Test Toast (Uncomment to test)
  // setTimeout(() => showToast('Chào mừng bạn quay lại!', 'gold'), 1000);
});

/* === PAGE LOADER === */
function initPageLoader() {
  const loader = document.getElementById("page-loader");
  if (loader) {
    window.addEventListener("load", () => {
      setTimeout(() => {
        loader.classList.add("hidden");
      }, 500); // Small delay for smoothness
    });
  }
}

/* === SCROLL TO TOP === */
function initScrollToTop() {
  const scrollBtn = document.getElementById("scroll-to-top");
  if (!scrollBtn) return;

  window.addEventListener("scroll", () => {
    if (window.scrollY > 300) {
      scrollBtn.classList.add("visible");
    } else {
      scrollBtn.classList.remove("visible");
    }
  });

  scrollBtn.addEventListener("click", () => {
    window.scrollTo({
      top: 0,
      behavior: "smooth",
    });
  });
}

/* === TOAST SYSTEM === */
function showToast(message, type = "info") {
  let container = document.getElementById("toast-container");
  if (!container) {
    container = document.createElement("div");
    container.id = "toast-container";
    document.body.appendChild(container);
  }

  // Create Toast Element
  const toast = document.createElement("div");
  toast.className = `toast-message toast-${type}`;

  // Icons based on type
  let iconClass = "bi-info-circle-fill";
  let titleText = "Thông báo";

  switch (type) {
    case "success":
      iconClass = "bi-check-circle-fill";
      titleText = "Thành công";
      break;
    case "error":
      iconClass = "bi-exclamation-circle-fill";
      titleText = "Lỗi";
      break;
    case "warning":
      iconClass = "bi-exclamation-triangle-fill";
      titleText = "Cảnh báo";
      break;
    case "gold":
      iconClass = "bi-stars";
      titleText = "Thông báo";
      break;
  }

  toast.innerHTML = `
        <div class="toast-icon">
            <i class="bi ${iconClass}"></i>
        </div>
        <div class="toast-content">
            <span class="toast-title">${titleText}</span>
            <span class="toast-body">${message}</span>
        </div>
        <button class="toast-close" onclick="this.parentElement.remove()">
            <i class="bi bi-x"></i>
        </button>
    `;

  container.appendChild(toast);

  // Animate In
  setTimeout(() => toast.classList.add("show"), 10);

  // Auto Dismiss
  setTimeout(() => {
    toast.classList.remove("show");
    setTimeout(() => toast.remove(), 400);
  }, 5000);
}

// Expose to global scope
window.showToast = showToast;
