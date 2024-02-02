using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Button = UnityEngine.UI.Button;
using Toggle = UnityEngine.UI.Toggle;

namespace Unisave.EmailAuthentication
{
    public class EmailAuthPanel : MonoBehaviour
    {
        // the two forms
        public GameObject loginForm;
        public GameObject registerForm;
        
        // login form controls
        public TMP_InputField loginEmailField;
        public TMP_InputField loginPasswordField;
        public Button loginButton;
        public GameObject loginSpinner;
        public TMP_Text loginErrorText;
        public Button gotoRegistrationButton;

        // registration form controls
        public TMP_InputField registerEmailField;
        public TMP_InputField registerPasswordField;
        public TMP_InputField registerConfirmPasswordField;
        public Toggle registerLegalTermsToggle;
        public TMP_Text registerLegalTermsText;
        public Button registerButton;
        public GameObject registerSpinner;
        public TMP_Text registerErrorText;
        public Button gotoLoginButton;
        
        // text messages
        public string msgLoginFailedGeneric
            = "Login failed.";
        public string msgInvalidLoginCredentials
            = "Given login credentials are not valid.";
        public string msgRegisterFailedGeneric
            = "Registration failed.";
        public string msgPasswordsDontMatch
            = "The two password fields differ.";
        public string msgEmailTaken
            = "This email has already been registered.";
        public string msgInvalidEmail
            = "This is not a valid email address.";
        public string msgWeakPassword
            = "Password needs to be at least 8 characters long.";
        public string msgLegalConsentRequired
            = "You need to accept the legal terms in order to register an account.";
        
        // events that fire when login (or registration) succeed
        [Serializable]
        public class LoginEvent : UnityEvent<EmailLoginResponse> { }
        
        [Serializable]
        public class RegisterEvent : UnityEvent<EmailRegisterResponse> { }
        
        public LoginEvent onLoginSuccess = new LoginEvent();
        
        public RegisterEvent onRegistrationSuccess = new RegisterEvent();

        // UI Event system, used for tab navigation
        // (automatically set to EventSystem.current if left at null)
        public EventSystem eventSystem;
        
        void Start()
        {
            CheckRequiredDependencies();

            if (eventSystem == null)
                eventSystem = EventSystem.current;
            
            loginButton.onClick.AddListener(OnLoginClicked);
            gotoRegistrationButton.onClick.AddListener(OnGotoRegisterClicked);
            registerButton.onClick.AddListener(OnRegisterClicked);
            gotoLoginButton.onClick.AddListener(OnGotoLoginClicked);
            
            loginEmailField.onSubmit.AddListener(_ => OnLoginClicked());
            loginPasswordField.onSubmit.AddListener(_ => OnLoginClicked());
            
            registerEmailField.onSubmit.AddListener(_ => OnRegisterClicked());
            registerPasswordField.onSubmit.AddListener(_ => OnRegisterClicked());
            registerConfirmPasswordField.onSubmit.AddListener(_ => OnRegisterClicked());
            
            ShowLoginForm();
        }

        private void CheckRequiredDependencies()
        {
            if (loginForm == null)
                throw new ArgumentException(
                    $"Link the '{nameof(loginForm)}' in the inspector."
                );
            
            if (registerForm == null)
                throw new ArgumentException(
                    $"Link the '{nameof(registerForm)}' in the inspector."
                );
            
            if (loginEmailField == null)
                throw new ArgumentException(
                    $"Link the '{nameof(loginEmailField)}' in the inspector."
                );
            
            if (loginPasswordField == null)
                throw new ArgumentException(
                    $"Link the '{nameof(loginPasswordField)}' in the inspector."
                );
            
            if (loginButton == null)
                throw new ArgumentException(
                    $"Link the '{nameof(loginButton)}' in the inspector."
                );
            
            if (loginSpinner == null)
                throw new ArgumentException(
                    $"Link the '{nameof(loginSpinner)}' in the inspector."
                );
            
            if (loginErrorText == null)
                throw new ArgumentException(
                    $"Link the '{nameof(loginErrorText)}' in the inspector."
                );
            
            if (gotoRegistrationButton == null)
                throw new ArgumentException(
                    $"Link the '{nameof(gotoRegistrationButton)}' in the inspector."
                );
            
            if (registerEmailField == null)
                throw new ArgumentException(
                    $"Link the '{nameof(registerEmailField)}' in the inspector."
                );
            
            if (registerPasswordField == null)
                throw new ArgumentException(
                    $"Link the '{nameof(registerPasswordField)}' in the inspector."
                );
            
            if (registerConfirmPasswordField == null)
                throw new ArgumentException(
                    $"Link the '{nameof(registerConfirmPasswordField)}' in the inspector."
                );
            
            if (registerLegalTermsToggle == null)
                throw new ArgumentException(
                    $"Link the '{nameof(registerLegalTermsToggle)}' in the inspector."
                );
            
            if (registerLegalTermsText == null)
                throw new ArgumentException(
                    $"Link the '{nameof(registerLegalTermsText)}' in the inspector."
                );
            
            if (registerButton == null)
                throw new ArgumentException(
                    $"Link the '{nameof(registerButton)}' in the inspector."
                );
            
            if (registerSpinner == null)
                throw new ArgumentException(
                    $"Link the '{nameof(registerSpinner)}' in the inspector."
                );
            
            if (registerErrorText == null)
                throw new ArgumentException(
                    $"Link the '{nameof(registerErrorText)}' in the inspector."
                );
            
            if (gotoLoginButton == null)
                throw new ArgumentException(
                    $"Link the '{nameof(gotoLoginButton)}' in the inspector."
                );
        }

        private void LateUpdate()
        {
            // opening links in the legal toggle label
            if (Input.GetMouseButtonDown(0)) // left mouse button
                HandlePotentialLinkClick();
            
            // tab navigation
            if (Input.GetKeyDown(KeyCode.Tab))
            {
                NavigateNext(
                    // This does not work with ISO_Left_Tab key, which is
                    // the Shift+Tab key event emitted on my i3wm, Ubuntu,
                    // XServer. Unity emits this as KeyCode.None, since it
                    // does not have this key implemented. But hopefully
                    // this will work on Windows, as many forum posts use it.
                    reversed: Input.GetKey(KeyCode.LeftShift)
                              || Input.GetKey(KeyCode.RightShift)
                );
            }
        }

        public void ShowLoginForm()
        {
            // show
            loginForm.SetActive(true);
            registerForm.SetActive(false);

            // reset
            loginEmailField.text = "";
            loginPasswordField.text = "";
            loginSpinner.SetActive(false);
            loginErrorText.gameObject.SetActive(false);
            loginErrorText.text = msgLoginFailedGeneric;
            loginSpinner.SetActive(false);

            // focus
            loginEmailField.Select();
            loginEmailField.ActivateInputField();
        }

        public void ShowRegisterForm()
        {
            // show
            loginForm.SetActive(false);
            registerForm.SetActive(true);
            
            // reset
            registerEmailField.text = "";
            registerPasswordField.text = "";
            registerConfirmPasswordField.text = "";
            registerErrorText.gameObject.SetActive(false);
            registerErrorText.text = msgRegisterFailedGeneric;
            registerSpinner.SetActive(false);
            
            // focus
            registerEmailField.Select();
            registerEmailField.ActivateInputField();
        }
        
        public async void OnLoginClicked()
        {
            loginSpinner.SetActive(true);

            EmailLoginResponse response = await this.LoginViaEmail(
                email: loginEmailField.text,
                password: loginPasswordField.text
            );
            
            loginSpinner.SetActive(false);

            if (response.Success)
            {
                onLoginSuccess?.Invoke(response);
                
                // reset to default looks
                ShowLoginForm();
                ForceRefreshElementSizes(loginForm);
            }
            else
            {
                loginErrorText.gameObject.SetActive(true);
                loginErrorText.text = msgInvalidLoginCredentials;
                ForceRefreshElementSizes(loginForm);
            }
        }

        public void OnGotoRegisterClicked()
        {
            ShowRegisterForm();
        }

        public async void OnRegisterClicked()
        {
            // check that the password confirmation matches
            if (registerPasswordField.text != registerConfirmPasswordField.text)
            {
                registerErrorText.gameObject.SetActive(true);
                registerErrorText.text = msgPasswordsDontMatch;
                ForceRefreshElementSizes(registerForm);
                return;
            }
            
            // send the registration request
            registerSpinner.SetActive(true);

            EmailRegisterResponse response = await this.RegisterViaEmail(
                email: registerEmailField.text,
                password: registerPasswordField.text,
                playerAcceptsLegalTerms: registerLegalTermsToggle.isOn
            );
            
            registerSpinner.SetActive(false);

            if (response.Success)
            {
                onRegistrationSuccess?.Invoke(response);
                
                // reset to default looks
                ShowLoginForm();
                ForceRefreshElementSizes(loginForm);
            }
            else
            {
                registerErrorText.gameObject.SetActive(true);

                switch (response.StatusCode)
                {
                    case EmailRegisterStatusCode.InvalidEmail:
                        registerErrorText.text = msgInvalidEmail;
                        break;
                    
                    case EmailRegisterStatusCode.WeakPassword:
                        registerErrorText.text = msgWeakPassword;
                        break;
                    
                    case EmailRegisterStatusCode.EmailTaken:
                        registerErrorText.text = msgEmailTaken;
                        break;
                    
                    case EmailRegisterStatusCode.LegalConsentRequired:
                        registerErrorText.text = msgLegalConsentRequired;
                        break;
                    
                    default:
                        registerErrorText.text = msgRegisterFailedGeneric;
                        break;
                }
                
                ForceRefreshElementSizes(registerForm);
            }
        }

        public void OnGotoLoginClicked()
        {
            ShowLoginForm();
        }

        private void ForceRefreshElementSizes(GameObject form)
        {
            // https://forum.unity.com/threads/content-size-fitter-refresh-problem.498536/
            Canvas.ForceUpdateCanvases();
            form.GetComponent<VerticalLayoutGroup>().enabled = false;
            form.GetComponent<VerticalLayoutGroup>().enabled = true;
        }
        
        private void HandlePotentialLinkClick()
        {
            if (!registerForm.activeSelf)
                return;
            
            int linkIndex = TMP_TextUtilities.FindIntersectingLink(
                registerLegalTermsText,
                Input.mousePosition,
                null // no camera for "screen space - overlay" canvas
            );
            
            if (linkIndex == -1)
                return;
             
            TMP_LinkInfo info = registerLegalTermsText.textInfo.linkInfo[linkIndex];
            
            // interpret ID as a URL to open
            string url = info.GetLinkID();
            
            Debug.Log("Opening URL: " + url);
            Application.OpenURL(url);
        }
        
        private void NavigateNext(bool reversed = false)
        {
            // get currently selected element
            GameObject current = eventSystem.currentSelectedGameObject;
            
            // if none, select first field and return
            if (current == null)
            {
                TMP_InputField firstField = loginForm.activeSelf
                    ? loginEmailField
                    : registerEmailField;
                firstField.Select();
                firstField.ActivateInputField();
                return;
            }
            
            // get the next element to become selected
            Selectable next = reversed
                ? current.GetComponent<Selectable>()?.FindSelectableOnUp()
                : current.GetComponent<Selectable>()?.FindSelectableOnDown();
            
            // if none, do nothing
            if (next == null)
                return;
            
            next.Select();
            
            // if input field, show caret
            if (next is TMP_InputField inputField)
                inputField.ActivateInputField();
        }
    }
}