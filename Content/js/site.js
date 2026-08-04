// Please see documentation at https://docs.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Password visibility toggle for all password fields.
(function () {
    // Resolve icons relative to this script so virtual-directory hosts (e.g. /ssp) work.
    function resolveContentRoot() {
        const current = document.currentScript && document.currentScript.src;
        if (current) {
            return current.replace(/\/js\/[^/?#]*([?#].*)?$/, '');
        }

        const scripts = document.getElementsByTagName('script');
        for (let i = scripts.length - 1; i >= 0; i--) {
            const src = scripts[i].src || '';
            if (/\/Content\/js\/site\.js([?#]|$)/i.test(src)) {
                return src.replace(/\/js\/[^/?#]*([?#].*)?$/, '');
            }
        }

        return '';
    }

    const contentRoot = resolveContentRoot();
    const eyeOpenIcon = contentRoot + '/images/password-eye-open.svg';
    const eyeClosedIcon = contentRoot + '/images/password-eye-closed.svg';

    function updateToggleState(input, button, icon) {
        const isPasswordVisible = input.type === 'text';
        icon.src = isPasswordVisible ? eyeOpenIcon : eyeClosedIcon;
        button.setAttribute('aria-pressed', isPasswordVisible ? 'true' : 'false');
        button.setAttribute('aria-label', isPasswordVisible ? 'Hide password' : 'Show password');
    }

    function positionToggleButton(input, button) {
        const top = input.offsetTop + (input.offsetHeight / 2);
        button.style.top = `${top}px`;
    }

    function initializePasswordToggles() {
        const passwordInputs = document.querySelectorAll('input[type="password"]');

        passwordInputs.forEach(function (input) {
            if (input.dataset.passwordToggleInitialized === "true") {
                return;
            }

            const host = input.closest('.input') || input.parentElement;
            if (!host) {
                return;
            }

            host.classList.add('password-toggle-host');
            input.classList.add('password-toggle-input');

            if (host.querySelector('.password-toggle-button')) {
                input.dataset.passwordToggleInitialized = "true";
                return;
            }

            const button = document.createElement('button');
            button.type = 'button';
            button.className = 'password-toggle-button';
            button.setAttribute('aria-pressed', 'false');
            button.setAttribute('aria-label', 'Show password');

            const icon = document.createElement('img');
            icon.alt = '';
            button.appendChild(icon);

            updateToggleState(input, button, icon);
            positionToggleButton(input, button);

            button.addEventListener('click', function () {
                const showPassword = input.type === 'password';
                input.type = showPassword ? 'text' : 'password';
                updateToggleState(input, button, icon);
                positionToggleButton(input, button);
            });

            window.addEventListener('resize', function () {
                positionToggleButton(input, button);
            });

            host.appendChild(button);
            input.dataset.passwordToggleInitialized = "true";
        });
    }

    function updateFloatingInputState(input) {
        const host = input.closest('.input');
        if (!host) {
            return;
        }

        const placeholder = input.getAttribute('placeholder');
        if (placeholder && !host.dataset.floatingLabel) {
            host.dataset.floatingLabel = placeholder;
        }

        const hasValue = input.value.trim().length > 0;
        const isFocused = document.activeElement === input;

        host.classList.toggle('input-has-value', hasValue);
        host.classList.toggle('input-is-focused', isFocused);
    }

    function updateInputValidationState(input) {
        const host = input.closest('.input');
        if (!host) {
            return;
        }

        const validationMessage = host.querySelector('.field-validation-error');
        const hasVisibleValidationError = validationMessage && validationMessage.textContent && validationMessage.textContent.trim().length > 0;
        const hasError = input.classList.contains('input-validation-error') || hasVisibleValidationError;
        host.classList.toggle('input-has-error', hasError);
    }

    function initializeFloatingInputs() {
        const textLikeInputs = document.querySelectorAll('.login .input input[type="text"], .login .input input[type="email"], .login .input input[type="password"]');

        textLikeInputs.forEach(function (input) {
            if (input.dataset.floatingInputInitialized === "true") {
                updateFloatingInputState(input);
                updateInputValidationState(input);
                return;
            }

            updateFloatingInputState(input);
            updateInputValidationState(input);
            input.addEventListener('input', function () { updateFloatingInputState(input); });
            input.addEventListener('focus', function () { updateFloatingInputState(input); });
            input.addEventListener('blur', function () {
                updateFloatingInputState(input);
                updateInputValidationState(input);
            });

            const validationObserver = new MutationObserver(function () {
                updateInputValidationState(input);
            });

            validationObserver.observe(input, {
                attributes: true,
                attributeFilter: ['class']
            });

            input.dataset.floatingInputInitialized = "true";
        });
    }

    function initializeUsernameHelpTooltips() {
        const helpAnchors = document.querySelectorAll('.username-help-anchor');

        helpAnchors.forEach(function (anchor) {
            if (anchor.dataset.usernameHelpInitialized === 'true') {
                return;
            }

            const button = anchor.querySelector('.username-help-button');
            const tooltip = anchor.querySelector('.username-help-tooltip');
            if (!button || !tooltip) {
                return;
            }

            const showTooltip = function () {
                anchor.classList.add('is-active');
                tooltip.classList.add('is-visible');
            };

            const hideTooltip = function () {
                anchor.classList.remove('is-active');
                tooltip.classList.remove('is-visible');
            };

            anchor.addEventListener('mouseenter', showTooltip);
            anchor.addEventListener('mouseleave', hideTooltip);
            button.addEventListener('focus', showTooltip);
            button.addEventListener('blur', hideTooltip);

            anchor.dataset.usernameHelpInitialized = 'true';
        });
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', function () {
            initializePasswordToggles();
            initializeFloatingInputs();
            initializeUsernameHelpTooltips();
        });
    } else {
        initializePasswordToggles();
        initializeFloatingInputs();
        initializeUsernameHelpTooltips();
    }

})();
