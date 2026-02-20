// using UnityEngine;
// using UnityEngine.XR.Interaction.Toolkit;
//
// [RequireComponent(typeof(XRSocketInteractor))]
// public class SocketReturnLinker : MonoBehaviour
// {
//     private XRSocketInteractor socket;
//
//     private void Awake() => socket = GetComponent<XRSocketInteractor>();
//
//     private void OnEnable()  => socket.selectEntered.AddListener(OnSocketGrab);
//     private void OnDisable() => socket.selectEntered.RemoveListener(OnSocketGrab);
//
//     private void OnSocketGrab(SelectEnterEventArgs args)
//     {
//         var item = args.interactableObject.transform.GetComponent<WaistSlotItem>();
//         if (item) item.homeSocket = socket;   // 注册为家
//     }
// }