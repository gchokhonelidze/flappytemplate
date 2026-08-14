using System.Collections;
using UnityEngine;

namespace FlappyTemplate
{
    // Drop this next to a DiceRoller - on the cube itself or anywhere in the scene with the cube dragged
    // into the field - and press play.
    //
    //   1..6    roll that exact value
    //   Space   roll a random value
    //   H       roll a random value with a hover at the top of the jump
    //   J       toggle the jump on and off
    //   A       roll 1 through 6 in order, so you can check every face of your mesh in one go
    //
    // Every key also has a public method behind it, so the same script wires straight onto UI buttons.
    [AddComponentMenu("FlappyBet/Dice Roller Example")]
    public class DiceRollerExample : MonoBehaviour
    {
        [SerializeField]
        private DiceRoller dice;

        [Tooltip("Hover used by the H key and RollHovering(), in seconds at the top of the jump.")]
        [SerializeField]
        private float hoverDuration = 2f;

        [Tooltip("Pause between rolls of the Audit Faces run.")]
        [SerializeField]
        private float auditDelay = 0.6f;

        [Tooltip("Ignore a roll request while one is already playing.")]
        [SerializeField]
        private bool blockWhileRolling = true;

        private Coroutine audit;

        private void Awake()
        {
            // Left empty in the inspector it falls back to a dice on this object, which is the common case
            // of dropping this straight onto the cube.
            if (dice == null)
                dice = GetComponent<DiceRoller>();
            if (dice == null)
                Debug.LogError($"{nameof(DiceRollerExample)}: no {nameof(DiceRoller)} assigned or found on this object.", this);
        }

        private void Update()
        {
            if (dice == null)
                return;

            // KeyCode.Alpha1 is 49 and the digits run in order, so value N sits at Alpha1 + (N - 1).
            for (int value = 1; value <= 6; value++)
            {
                if (Input.GetKeyDown(KeyCode.Alpha1 + value - 1))
                    Roll(value);
            }

            if (Input.GetKeyDown(KeyCode.Space))
                RollRandom();

            if (Input.GetKeyDown(KeyCode.H))
                RollHovering(Random.Range(1, 7));

            if (Input.GetKeyDown(KeyCode.J))
                ToggleJump();

            if (Input.GetKeyDown(KeyCode.A))
                AuditFaces();
        }

        // The one call that matters: the value is decided here, before a single frame of animation, and
        // the dice is guaranteed to come to rest showing it.
        public void Roll(int value)
        {
            if (dice == null)
                return;
            if (blockWhileRolling && dice.IsRolling)
                return;

            dice.Roll(value, () => Debug.Log($"Dice settled on {value}.", this));
        }

        public void RollRandom() => Roll(Random.Range(1, 7));

        // The hover passed here wins over the roller's own default, so one dice can do a quick roll and a
        // long suspenseful one from two different buttons.
        public void RollHovering(int value)
        {
            if (dice == null)
                return;
            if (blockWhileRolling && dice.IsRolling)
                return;

            dice.Roll(value, hoverDuration, () => Debug.Log($"Dice settled on {value} after a {hoverDuration:0.##}s hover.", this));
        }

        public void ToggleJump()
        {
            if (dice == null)
                return;

            dice.ToggleJump();
            Debug.Log($"Jump {(dice.JumpEnabled ? "enabled" : "disabled")}.", this);
        }

        // Walks all six values in order. Worth running once against a new dice mesh: if a face comes up
        // wrong, that value's entry in the roller's faceOrientations table is the one to fix.
        public void AuditFaces()
        {
            if (dice == null)
                return;
            if (audit != null)
                StopCoroutine(audit);
            audit = StartCoroutine(AuditRoutine());
        }

        private IEnumerator AuditRoutine()
        {
            for (int value = 1; value <= 6; value++)
            {
                bool settled = false;
                // Bypasses Roll() on purpose - the audit drives the rolls back to back and must not be
                // dropped by the blockWhileRolling guard.
                dice.Roll(value, () => settled = true);
                // A null tween means the value has no orientation configured; the roller has already logged
                // it, so just move on rather than waiting forever on a callback that will never fire.
                yield return new WaitUntil(() => settled || !dice.IsRolling);
                Debug.Log($"Audit: face {value} should be up now.", this);
                yield return new WaitForSeconds(auditDelay);
            }

            audit = null;
        }
    }
}
