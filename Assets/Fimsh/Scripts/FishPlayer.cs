using RinCore;
using UnityEngine;
using UnityEngine.InputSystem;
public class FishPlayer : MonoBehaviour
{
    [SerializeField] Transform rotateAnchor;
    float rotation;
    [SerializeField] float focusSpeed = 140f, unfocusSpeed = 240f;
    [SerializeField] InputActionReference focusAction;
    [SerializeField] Collider netHitbox;
    [SerializeField] DialogueCharacterSO playerCharacterDialogue;
    public Vector3 netColliderCenter => netHitbox != null ? netHitbox.bounds.center : transform.position;
    private void Start()
    {
        Dialogue.TrySetPlayerCharacter(playerCharacterDialogue);
    }
    private void Update()
    {
        float moveXTarget = GenericInput.Move.QuantizeToStepSize(45f).x;
        rotation = (rotation + moveXTarget.Multiply(Time.deltaTime * (focusAction.IsPressed() ? focusSpeed : unfocusSpeed))).Clamp(-45f, 45f);
        rotateAnchor.rotation = Quaternion.Euler(0f, rotation, 0f);
    }
}
