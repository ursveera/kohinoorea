// Auth Tab Switching
(function() {
  'use strict';

  function switchToTab(tabName, allTabs, signinContent, signupContent) {
    allTabs.forEach(t => t.classList.remove('active'));
    const target = document.querySelector(`.auth-tab[data-tab="${tabName}"]`);
    if (target) target.classList.add('active');

    if (tabName === 'signin') {
      signinContent.style.display = 'flex';
      signupContent.style.display = 'none';
    } else {
      signinContent.style.display = 'none';
      signupContent.style.display = 'flex';
    }
  }

  function initAuthTabs() {
    const tabs = document.querySelectorAll('.auth-tab');
    const signinContent = document.getElementById('signin-content');
    const signupContent = document.getElementById('signup-content');
    
    if (!tabs.length || !signinContent || !signupContent) {
      return false;
    }

    // Remove old event listeners by cloning
    tabs.forEach(tab => {
      const newTab = tab.cloneNode(true);
      tab.parentNode.replaceChild(newTab, tab);
    });

    const newTabs = document.querySelectorAll('.auth-tab');

    newTabs.forEach(tab => {
      tab.addEventListener('click', function(e) {
        e.preventDefault();
        switchToTab(this.getAttribute('data-tab'), newTabs, signinContent, signupContent);
      });
    });

    // Segment role selector
    const segmentBtns = document.querySelectorAll('.role-segment-btn');
    const submitText = document.querySelector('.auth-submit-button .submit-text');

    segmentBtns.forEach(btn => {
      btn.addEventListener('click', function() {
        segmentBtns.forEach(b => b.classList.remove('active'));
        this.classList.add('active');
        if (submitText) {
          const role = this.getAttribute('data-role');
          submitText.textContent = `Sign in as ${role.charAt(0).toUpperCase() + role.slice(1)}`;
        }
      });
    });

    // "Sign up free" link inside sign-in panel
    const signupLink = document.querySelector('[data-switch-tab="signup"]');
    if (signupLink) {
      signupLink.addEventListener('click', function(e) {
        e.preventDefault();
        switchToTab('signup', newTabs, signinContent, signupContent);
      });
    }

    return true;
  }

  window.kohinoorAuthTabsInit = initAuthTabs;

  if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', initAuthTabs);
  } else {
    initAuthTabs();
  }

  let retryCount = 0;
  const retryInterval = setInterval(() => {
    if (retryCount++ >= 20) { clearInterval(retryInterval); return; }
    if (initAuthTabs()) clearInterval(retryInterval);
  }, 100);
})();

