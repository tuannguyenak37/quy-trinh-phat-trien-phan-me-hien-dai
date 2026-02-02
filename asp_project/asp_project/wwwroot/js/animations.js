/* ============================================
   MALIKETH FASHION - ANIMATIONS & INTERACTIONS
   Advanced JavaScript for artistic effects
   ============================================ */

document.addEventListener("DOMContentLoaded", function () {
  // ============================================
  // 1. NAVBAR SCROLL EFFECT
  // ============================================
  const navbar = document.querySelector(".navbar");
  let lastScrollY = window.scrollY;

  function updateNavbar() {
    const currentScrollY = window.scrollY;

    if (currentScrollY > 50) {
      navbar.classList.add("navbar-scrolled");
    } else {
      navbar.classList.remove("navbar-scrolled");
    }

    // Hide/Show on scroll direction
    if (currentScrollY > lastScrollY && currentScrollY > 200) {
      navbar.classList.add("navbar-hidden");
    } else {
      navbar.classList.remove("navbar-hidden");
    }

    lastScrollY = currentScrollY;
  }

  window.addEventListener("scroll", () => {
    requestAnimationFrame(updateNavbar);
  });

  // ============================================
  // 2. SCROLL-TRIGGERED ANIMATIONS
  // ============================================
  const animateOnScrollElements = document.querySelectorAll(
    ".scroll-animate, .product-card, .category-item",
  );

  const observerOptions = {
    root: null,
    rootMargin: "0px 0px -100px 0px",
    threshold: 0.1,
  };

  const animationObserver = new IntersectionObserver((entries) => {
    entries.forEach((entry, index) => {
      if (entry.isIntersecting) {
        // Staggered animation
        setTimeout(() => {
          entry.target.classList.add("animate-in");
        }, index * 100);

        animationObserver.unobserve(entry.target);
      }
    });
  }, observerOptions);

  animateOnScrollElements.forEach((element) => {
    element.classList.add("scroll-animate");
    animationObserver.observe(element);
  });

  // ============================================
  // 3. PARALLAX EFFECT FOR HERO
  // ============================================
  const heroSection = document.querySelector(
    ".hero-banner, .carousel-item.active",
  );

  if (heroSection) {
    window.addEventListener("scroll", () => {
      requestAnimationFrame(() => {
        const scrolled = window.scrollY;
        const heroImage = heroSection.querySelector("img");
        if (heroImage && scrolled < window.innerHeight) {
          heroImage.style.transform = `translateY(${scrolled * 0.3}px) scale(1.1)`;
        }
      });
    });
  }

  // ============================================
  // 4. SMOOTH SCROLL TO TOP BUTTON
  // ============================================
  const scrollToTopBtn = document.createElement("button");
  scrollToTopBtn.innerHTML = '<i class="bi bi-arrow-up"></i>';
  scrollToTopBtn.className = "scroll-to-top-btn";
  scrollToTopBtn.setAttribute("aria-label", "Scroll to top");
  document.body.appendChild(scrollToTopBtn);

  // Add styles dynamically
  scrollToTopBtn.style.cssText = `
        position: fixed;
        bottom: 30px;
        right: 30px;
        width: 50px;
        height: 50px;
        background: linear-gradient(135deg, #D4AF37 0%, #E6C55B 100%);
        color: #0A0A0A;
        border: none;
        border-radius: 50%;
        cursor: pointer;
        opacity: 0;
        visibility: hidden;
        transform: translateY(20px);
        transition: all 0.3s ease;
        z-index: 9999;
        font-size: 1.25rem;
        display: flex;
        align-items: center;
        justify-content: center;
        box-shadow: 0 4px 20px rgba(212, 175, 55, 0.3);
    `;

  window.addEventListener("scroll", () => {
    if (window.scrollY > 500) {
      scrollToTopBtn.style.opacity = "1";
      scrollToTopBtn.style.visibility = "visible";
      scrollToTopBtn.style.transform = "translateY(0)";
    } else {
      scrollToTopBtn.style.opacity = "0";
      scrollToTopBtn.style.visibility = "hidden";
      scrollToTopBtn.style.transform = "translateY(20px)";
    }
  });

  scrollToTopBtn.addEventListener("click", () => {
    window.scrollTo({
      top: 0,
      behavior: "smooth",
    });
  });

  scrollToTopBtn.addEventListener("mouseenter", () => {
    scrollToTopBtn.style.transform = "translateY(-5px)";
    scrollToTopBtn.style.boxShadow = "0 8px 30px rgba(212, 175, 55, 0.5)";
  });

  scrollToTopBtn.addEventListener("mouseleave", () => {
    scrollToTopBtn.style.transform = "translateY(0)";
    scrollToTopBtn.style.boxShadow = "0 4px 20px rgba(212, 175, 55, 0.3)";
  });

  // ============================================
  // 5. IMAGE LAZY LOADING WITH FADE
  // ============================================
  const lazyImages = document.querySelectorAll("img[data-src]");

  const imageObserver = new IntersectionObserver((entries) => {
    entries.forEach((entry) => {
      if (entry.isIntersecting) {
        const img = entry.target;
        img.style.opacity = "0";
        img.src = img.dataset.src;
        img.onload = () => {
          img.style.transition = "opacity 0.5s ease";
          img.style.opacity = "1";
        };
        img.removeAttribute("data-src");
        imageObserver.unobserve(img);
      }
    });
  });

  lazyImages.forEach((img) => imageObserver.observe(img));

  // ============================================
  // 6. CART BADGE BOUNCE ANIMATION
  // ============================================
  const cartBadge = document.getElementById("cart-badge");

  window.addEventListener("cartUpdated", () => {
    if (cartBadge) {
      cartBadge.style.animation = "none";
      cartBadge.offsetHeight; // Trigger reflow
      cartBadge.style.animation = "bounceIn 0.5s ease";
    }
  });

  // ============================================
  // 7. PRODUCT CARD QUICK ACTIONS
  // ============================================
  const productCards = document.querySelectorAll(".product-card");

  productCards.forEach((card) => {
    // Create quick action overlay if not exists
    const imgWrapper = card.querySelector(".card-img-wrapper");
    if (imgWrapper && !imgWrapper.querySelector(".quick-actions")) {
      const quickActions = document.createElement("div");
      quickActions.className = "quick-actions";
      quickActions.innerHTML = `
                <button class="quick-action-btn wishlist-btn" title="Thêm vào yêu thích">
                    <i class="bi bi-heart"></i>
                </button>
                <button class="quick-action-btn quick-view-btn" title="Xem nhanh">
                    <i class="bi bi-eye"></i>
                </button>
            `;
      imgWrapper.appendChild(quickActions);
    }
  });

  // Handle wishlist click
  document.addEventListener("click", (e) => {
    if (e.target.closest(".wishlist-btn")) {
      const btn = e.target.closest(".wishlist-btn");
      const icon = btn.querySelector("i");

      if (icon.classList.contains("bi-heart")) {
        icon.classList.remove("bi-heart");
        icon.classList.add("bi-heart-fill");
        btn.style.color = "#C62828";

        // Add pulse animation
        btn.style.animation = "pulse 0.3s ease";
        setTimeout(() => {
          btn.style.animation = "";
        }, 300);
      } else {
        icon.classList.remove("bi-heart-fill");
        icon.classList.add("bi-heart");
        btn.style.color = "";
      }

      e.preventDefault();
      e.stopPropagation();
    }
  });

  // ============================================
  // 8. CAROUSEL ENHANCED TRANSITIONS
  // ============================================
  const heroCarousel = document.getElementById("heroCarousel");

  if (heroCarousel) {
    heroCarousel.addEventListener("slide.bs.carousel", (event) => {
      const activeItem = event.relatedTarget;
      const caption = activeItem.querySelector(".carousel-caption");

      if (caption) {
        caption.style.opacity = "0";
        caption.style.transform = "translateY(30px)";
      }
    });

    heroCarousel.addEventListener("slid.bs.carousel", (event) => {
      const activeItem = event.relatedTarget;
      const caption = activeItem.querySelector(".carousel-caption");

      if (caption) {
        setTimeout(() => {
          caption.style.transition = "all 0.6s ease";
          caption.style.opacity = "1";
          caption.style.transform = "translateY(0)";
        }, 100);
      }
    });
  }

  // ============================================
  // 9. CATEGORY HOVER EFFECTS
  // ============================================
  const categoryItems = document.querySelectorAll(".category-item");

  categoryItems.forEach((item) => {
    item.addEventListener("mouseenter", () => {
      const img = item.querySelector("img");
      if (img) {
        img.style.transform = "scale(1.1) rotate(5deg)";
      }
    });

    item.addEventListener("mouseleave", () => {
      const img = item.querySelector("img");
      if (img) {
        img.style.transform = "scale(1) rotate(0deg)";
      }
    });
  });

  // ============================================
  // 10. SEARCH INPUT FOCUS EFFECT
  // ============================================
  const searchInput = document.querySelector(".search-input");
  const searchForm = document.querySelector(".search-form");

  if (searchInput && searchForm) {
    searchInput.addEventListener("focus", () => {
      searchForm.style.transform = "scale(1.02)";
      searchForm.style.boxShadow = "0 0 0 3px rgba(212, 175, 55, 0.2)";
    });

    searchInput.addEventListener("blur", () => {
      searchForm.style.transform = "scale(1)";
      searchForm.style.boxShadow = "none";
    });
  }

  // ============================================
  // 11. RIPPLE EFFECT FOR BUTTONS
  // ============================================
  function createRipple(event) {
    const button = event.currentTarget;
    const ripple = document.createElement("span");
    const rect = button.getBoundingClientRect();

    const size = Math.max(rect.width, rect.height);
    const x = event.clientX - rect.left - size / 2;
    const y = event.clientY - rect.top - size / 2;

    ripple.style.cssText = `
            position: absolute;
            width: ${size}px;
            height: ${size}px;
            left: ${x}px;
            top: ${y}px;
            background: rgba(255, 255, 255, 0.3);
            border-radius: 50%;
            transform: scale(0);
            animation: ripple 0.6s linear;
            pointer-events: none;
        `;

    button.style.position = "relative";
    button.style.overflow = "hidden";
    button.appendChild(ripple);

    setTimeout(() => ripple.remove(), 600);
  }

  // Apply ripple to specific buttons
  const rippleButtons = document.querySelectorAll(
    ".btn-ripple, .btn-primary, .btn-outline-primary, .btn-outline-danger",
  );
  rippleButtons.forEach((btn) => {
    btn.addEventListener("click", createRipple);
  });

  // ============================================
  // 12. STAGGERED ANIMATION FOR GRIDS
  // ============================================
  function staggerAnimation(elements, delay = 100) {
    elements.forEach((element, index) => {
      element.style.opacity = "0";
      element.style.transform = "translateY(20px)";

      setTimeout(() => {
        element.style.transition = "all 0.4s ease";
        element.style.opacity = "1";
        element.style.transform = "translateY(0)";
      }, index * delay);
    });
  }

  // Apply to product grids when they come into view
  const productGrids = document.querySelectorAll(
    ".row-cols-lg-5, .category-grid",
  );

  productGrids.forEach((grid) => {
    const gridObserver = new IntersectionObserver(
      (entries) => {
        entries.forEach((entry) => {
          if (entry.isIntersecting) {
            const items = entry.target.children;
            staggerAnimation(Array.from(items), 80);
            gridObserver.unobserve(entry.target);
          }
        });
      },
      { threshold: 0.2 },
    );

    gridObserver.observe(grid);
  });

  // ============================================
  // 13. SMOOTH HOVER FOR NAVBAR BRAND
  // ============================================
  const navbarBrand = document.querySelector(".navbar-brand-custom");

  if (navbarBrand) {
    navbarBrand.addEventListener("mouseenter", () => {
      navbarBrand.style.letterSpacing = "2px";
    });

    navbarBrand.addEventListener("mouseleave", () => {
      navbarBrand.style.letterSpacing = "normal";
    });
  }

  // ============================================
  // 14. COUNTER ANIMATION FOR STATS
  // ============================================
  function animateCounter(element, target, duration = 2000) {
    let start = 0;
    const increment = target / (duration / 16);

    function updateCounter() {
      start += increment;
      if (start < target) {
        element.textContent = Math.floor(start).toLocaleString();
        requestAnimationFrame(updateCounter);
      } else {
        element.textContent = target.toLocaleString();
      }
    }

    updateCounter();
  }

  // Apply to elements with data-counter attribute
  const counters = document.querySelectorAll("[data-counter]");
  counters.forEach((counter) => {
    const counterObserver = new IntersectionObserver((entries) => {
      entries.forEach((entry) => {
        if (entry.isIntersecting) {
          const target = parseInt(entry.target.dataset.counter);
          animateCounter(entry.target, target);
          counterObserver.unobserve(entry.target);
        }
      });
    });
    counterObserver.observe(counter);
  });

  console.log("✨ Maliketh Fashion animations loaded successfully!");
});

// ============================================
// GLOBAL UTILITY FUNCTIONS
// ============================================

// Debounce function
function debounce(func, wait) {
  let timeout;
  return function executedFunction(...args) {
    const later = () => {
      clearTimeout(timeout);
      func(...args);
    };
    clearTimeout(timeout);
    timeout = setTimeout(later, wait);
  };
}

// Throttle function
function throttle(func, limit) {
  let inThrottle;
  return function (...args) {
    if (!inThrottle) {
      func.apply(this, args);
      inThrottle = true;
      setTimeout(() => (inThrottle = false), limit);
    }
  };
}
