//------------------------------------------------------------------------------
// <auto-generated>
//     This code was auto-generated by com.unity.inputsystem:InputActionCodeGenerator
//     version 1.9.0
//     from Assets/InputSystem/ProbeMetaControls.inputactions
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;
using UnityEngine;

public partial class @ProbeMetaControls: IInputActionCollection2, IDisposable
{
    public InputActionAsset asset { get; }
    public @ProbeMetaControls()
    {
        asset = InputActionAsset.FromJson(@"{
    ""name"": ""ProbeMetaControls"",
    ""maps"": [
        {
            ""name"": ""ProbeMetaControl"",
            ""id"": ""49e6acce-5ef4-408e-a8fa-bc4787423202"",
            ""actions"": [
                {
                    ""name"": ""NextProbe"",
                    ""type"": ""Button"",
                    ""id"": ""108721c3-f35f-49f2-9241-7e0216cc018f"",
                    ""expectedControlType"": ""Button"",
                    ""processors"": """",
                    ""interactions"": """",
                    ""initialStateCheck"": false
                },
                {
                    ""name"": ""PrevProbe"",
                    ""type"": ""Button"",
                    ""id"": ""e5f1fc89-463d-4706-8808-b52f36a687a8"",
                    ""expectedControlType"": ""Button"",
                    ""processors"": """",
                    ""interactions"": """",
                    ""initialStateCheck"": false
                },
                {
                    ""name"": ""SwitchAxisMode"",
                    ""type"": ""Button"",
                    ""id"": ""1d360e34-a12a-492a-8082-001ccd73c45d"",
                    ""expectedControlType"": ""Button"",
                    ""processors"": """",
                    ""interactions"": """",
                    ""initialStateCheck"": false
                }
            ],
            ""bindings"": [
                {
                    ""name"": """",
                    ""id"": ""98c5463b-d1a0-4efa-a3f6-5e8429d25d84"",
                    ""path"": ""<Gamepad>/rightShoulder"",
                    ""interactions"": ""Tap"",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""NextProbe"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""1fe4a68a-d755-474e-ab44-5e374bb15777"",
                    ""path"": ""<Keyboard>/m"",
                    ""interactions"": ""Tap"",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""NextProbe"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""d00557a5-21a2-4dd4-b7bb-5947865ff4f8"",
                    ""path"": ""<Gamepad>/leftShoulder"",
                    ""interactions"": ""Tap"",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""PrevProbe"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""25de11d4-83e3-4b5e-90cf-5ae605c2ecc2"",
                    ""path"": ""<Keyboard>/n"",
                    ""interactions"": ""Tap"",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""PrevProbe"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""d3c8b135-b617-4544-a621-f79112d4ad87"",
                    ""path"": ""<Gamepad>/buttonSouth"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""SwitchAxisMode"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""8e0fb31b-16bc-425f-b1de-f8c1fadca630"",
                    ""path"": ""<Keyboard>/i"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""SwitchAxisMode"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                }
            ]
        }
    ],
    ""controlSchemes"": []
}");
        // ProbeMetaControl
        m_ProbeMetaControl = asset.FindActionMap("ProbeMetaControl", throwIfNotFound: true);
        m_ProbeMetaControl_NextProbe = m_ProbeMetaControl.FindAction("NextProbe", throwIfNotFound: true);
        m_ProbeMetaControl_PrevProbe = m_ProbeMetaControl.FindAction("PrevProbe", throwIfNotFound: true);
        m_ProbeMetaControl_SwitchAxisMode = m_ProbeMetaControl.FindAction("SwitchAxisMode", throwIfNotFound: true);
    }

    ~@ProbeMetaControls()
    {
        Debug.Assert(!m_ProbeMetaControl.enabled, "This will cause a leak and performance issues, ProbeMetaControls.ProbeMetaControl.Disable() has not been called.");
    }

    public void Dispose()
    {
        UnityEngine.Object.Destroy(asset);
    }

    public InputBinding? bindingMask
    {
        get => asset.bindingMask;
        set => asset.bindingMask = value;
    }

    public ReadOnlyArray<InputDevice>? devices
    {
        get => asset.devices;
        set => asset.devices = value;
    }

    public ReadOnlyArray<InputControlScheme> controlSchemes => asset.controlSchemes;

    public bool Contains(InputAction action)
    {
        return asset.Contains(action);
    }

    public IEnumerator<InputAction> GetEnumerator()
    {
        return asset.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public void Enable()
    {
        asset.Enable();
    }

    public void Disable()
    {
        asset.Disable();
    }

    public IEnumerable<InputBinding> bindings => asset.bindings;

    public InputAction FindAction(string actionNameOrId, bool throwIfNotFound = false)
    {
        return asset.FindAction(actionNameOrId, throwIfNotFound);
    }

    public int FindBinding(InputBinding bindingMask, out InputAction action)
    {
        return asset.FindBinding(bindingMask, out action);
    }

    // ProbeMetaControl
    private readonly InputActionMap m_ProbeMetaControl;
    private List<IProbeMetaControlActions> m_ProbeMetaControlActionsCallbackInterfaces = new List<IProbeMetaControlActions>();
    private readonly InputAction m_ProbeMetaControl_NextProbe;
    private readonly InputAction m_ProbeMetaControl_PrevProbe;
    private readonly InputAction m_ProbeMetaControl_SwitchAxisMode;
    public struct ProbeMetaControlActions
    {
        private @ProbeMetaControls m_Wrapper;
        public ProbeMetaControlActions(@ProbeMetaControls wrapper) { m_Wrapper = wrapper; }
        public InputAction @NextProbe => m_Wrapper.m_ProbeMetaControl_NextProbe;
        public InputAction @PrevProbe => m_Wrapper.m_ProbeMetaControl_PrevProbe;
        public InputAction @SwitchAxisMode => m_Wrapper.m_ProbeMetaControl_SwitchAxisMode;
        public InputActionMap Get() { return m_Wrapper.m_ProbeMetaControl; }
        public void Enable() { Get().Enable(); }
        public void Disable() { Get().Disable(); }
        public bool enabled => Get().enabled;
        public static implicit operator InputActionMap(ProbeMetaControlActions set) { return set.Get(); }
        public void AddCallbacks(IProbeMetaControlActions instance)
        {
            if (instance == null || m_Wrapper.m_ProbeMetaControlActionsCallbackInterfaces.Contains(instance)) return;
            m_Wrapper.m_ProbeMetaControlActionsCallbackInterfaces.Add(instance);
            @NextProbe.started += instance.OnNextProbe;
            @NextProbe.performed += instance.OnNextProbe;
            @NextProbe.canceled += instance.OnNextProbe;
            @PrevProbe.started += instance.OnPrevProbe;
            @PrevProbe.performed += instance.OnPrevProbe;
            @PrevProbe.canceled += instance.OnPrevProbe;
            @SwitchAxisMode.started += instance.OnSwitchAxisMode;
            @SwitchAxisMode.performed += instance.OnSwitchAxisMode;
            @SwitchAxisMode.canceled += instance.OnSwitchAxisMode;
        }

        private void UnregisterCallbacks(IProbeMetaControlActions instance)
        {
            @NextProbe.started -= instance.OnNextProbe;
            @NextProbe.performed -= instance.OnNextProbe;
            @NextProbe.canceled -= instance.OnNextProbe;
            @PrevProbe.started -= instance.OnPrevProbe;
            @PrevProbe.performed -= instance.OnPrevProbe;
            @PrevProbe.canceled -= instance.OnPrevProbe;
            @SwitchAxisMode.started -= instance.OnSwitchAxisMode;
            @SwitchAxisMode.performed -= instance.OnSwitchAxisMode;
            @SwitchAxisMode.canceled -= instance.OnSwitchAxisMode;
        }

        public void RemoveCallbacks(IProbeMetaControlActions instance)
        {
            if (m_Wrapper.m_ProbeMetaControlActionsCallbackInterfaces.Remove(instance))
                UnregisterCallbacks(instance);
        }

        public void SetCallbacks(IProbeMetaControlActions instance)
        {
            foreach (var item in m_Wrapper.m_ProbeMetaControlActionsCallbackInterfaces)
                UnregisterCallbacks(item);
            m_Wrapper.m_ProbeMetaControlActionsCallbackInterfaces.Clear();
            AddCallbacks(instance);
        }
    }
    public ProbeMetaControlActions @ProbeMetaControl => new ProbeMetaControlActions(this);
    public interface IProbeMetaControlActions
    {
        void OnNextProbe(InputAction.CallbackContext context);
        void OnPrevProbe(InputAction.CallbackContext context);
        void OnSwitchAxisMode(InputAction.CallbackContext context);
    }
}
