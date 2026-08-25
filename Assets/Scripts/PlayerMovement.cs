using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(Animator))]
public class PlayerMovement : MonoBehaviour
{
    private CharacterController controller;
    private Animator animator;
    private Health playerHealth;

    [Header("Configuración")]
    public float walkSpeed = 3f;
    public float runSpeed = 6f;
    public float rotationSpeed = 15f;
    public float gravity = -9.81f;

    [Header("Attack Lunge Settings")]
    public float lungeSpeed = 3f;
    public float lungeDuration = 0.15f;

    [Header("Combat Idle Settings")]
    public float combatModeDuration = 4f;
    private float combatTimer;
    private bool isInCombat = false;

    [Header("Efectos Visuales")]
    public TrailRenderer[] punchTrails;
    public ParticleSystem rustDust;
    public Renderer[] characterRenderers;
    public float flashInterval = 0.08f;

    [Header("Hitboxes de Combate (Desarmado)")]
    public Hitbox leftHandHitbox;
    public Hitbox rightHandHitbox;

    public enum WeaponType
    {
        Unarmed = 0,
        Pipe = 1,
        Revolver = 2
    }

    [Header("Configuración de Armas")]
    public WeaponType currentWeapon = WeaponType.Unarmed;
    public GameObject pipeModel;
    public GameObject revolverModel;
    public Hitbox pipeHitbox;
    public RevolverController revolverController;

    [Header("Inventario de Armas")]
    public bool hasPipe = false;
    public bool hasRevolver = false;

    [Header("Configuración de I-Frames y Esquive")]
    public float dodgeTotalDuration = 0.5f;
    public float iFrameDuration = 0.3f;
    public float iFrameStartDelay = 0.05f;
    public float dodgeCooldown = 0.7f;

    private float currentDodgeCooldown = 0f;

    public float mercyInvincibilityDuration = 1.5f;
    public bool isInvulnerable { get; private set; } = false;

    [Header("Estado del Jugador")]
    public bool isDead = false;

    [Range(0f, 1f)]
    public float injuredThreshold = 0.3f;

    [Header("Input Buffering")]
    public float inputBufferTime = 0.15f;

    private enum ActionType
    {
        None,
        Attack,
        Dodge
    }

    private ActionType bufferedAction = ActionType.None;
    private float bufferTimer = 0f;

    private bool isStationaryAttacking = false;
    private bool isMovingAttacking = false;
    private bool isDodging = false;
    private bool isPickingUp = false;

    private Coroutine attackCoroutine;
    private Coroutine lungeCoroutine;

    [Header("Tiempos de Ataque")]
    public float fistAttackDuration = 0.4f;
    public float weaponAttackDuration = 1.0f;
    public float shootDuration = 0.5f;
    public float fistComboDuration = 0.6f;
    public float weaponComboDuration = 1.2f;
    public float shootComboDuration = 0.6f;

    private float verticalVelocity;

    // ============================================================
    // NEW INPUT SYSTEM
    // ============================================================

    [Header("New Input System")]
    public InputActionReference moveAction;
    public InputActionReference runAction;
    public InputActionReference attackAction;
    public InputActionReference dodgeAction;

    [Header("Cambio de Armas")]
    public InputActionReference weapon1Action;
    public InputActionReference weapon2Action;
    public InputActionReference weapon3Action;
    public InputActionReference nextWeaponAction;
    public InputActionReference previousWeaponAction;

    [Header("Mouse")]
    public InputActionReference mousePositionAction;

    // ============================================================
    // UNITY
    // ============================================================

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();
        playerHealth = GetComponent<Health>();
    }

    void OnEnable()
    {
        EnableInputAction(moveAction);
        EnableInputAction(runAction);
        EnableInputAction(attackAction);
        EnableInputAction(dodgeAction);

        EnableInputAction(weapon1Action);
        EnableInputAction(weapon2Action);
        EnableInputAction(weapon3Action);

        EnableInputAction(nextWeaponAction);
        EnableInputAction(previousWeaponAction);

        EnableInputAction(mousePositionAction);
    }

    void OnDisable()
    {
        DisableInputAction(moveAction);
        DisableInputAction(runAction);
        DisableInputAction(attackAction);
        DisableInputAction(dodgeAction);

        DisableInputAction(weapon1Action);
        DisableInputAction(weapon2Action);
        DisableInputAction(weapon3Action);

        DisableInputAction(nextWeaponAction);
        DisableInputAction(previousWeaponAction);

        DisableInputAction(mousePositionAction);
    }

    void Start()
    {
        SetTrails(false);
        SetHitboxes(false);
        UpdateWeaponVisibility();
    }

    void Update()
    {
        if (isDead)
        {
            ApplyGravity();
            return;
        }

        // ========================================================
        // ESTADO HERIDO
        // ========================================================

        if (playerHealth != null)
        {
            bool isInjured =
                playerHealth.currentHealth <=
                (playerHealth.maxHealth * injuredThreshold);

            animator.SetBool("IsInjured", isInjured);
        }

        animator.SetInteger("WeaponType", (int)currentWeapon);

        // ========================================================
        // INPUT BUFFER
        // ========================================================

        if (bufferTimer > 0f)
        {
            bufferTimer -= Time.deltaTime;

            if (bufferTimer <= 0f)
            {
                bufferedAction = ActionType.None;
            }
        }

        // ========================================================
        // DODGE COOLDOWN
        // ========================================================

        if (currentDodgeCooldown > 0f)
        {
            currentDodgeCooldown -= Time.deltaTime;
        }

        // ========================================================
        // CAMBIO DE ARMAS
        // ========================================================

        if (!isStationaryAttacking &&
            !isMovingAttacking &&
            !isDodging &&
            !isPickingUp)
        {
            if (WasPressed(weapon1Action))
            {
                ChangeWeapon(WeaponType.Unarmed);
            }
            else if (WasPressed(weapon2Action) && hasPipe)
            {
                ChangeWeapon(WeaponType.Pipe);
            }
            else if (WasPressed(weapon3Action) && hasRevolver)
            {
                ChangeWeapon(WeaponType.Revolver);
            }

            if (WasPressed(nextWeaponAction))
            {
                CycleWeapon(1);
            }
            else if (WasPressed(previousWeaponAction))
            {
                CycleWeapon(-1);
            }
        }

        // ========================================================
        // DODGE
        // ========================================================

        if (WasPressed(dodgeAction))
        {
            if (isPickingUp)
                return;

            if (currentDodgeCooldown <= 0f && !isDodging)
            {
                if (isStationaryAttacking || isMovingAttacking)
                {
                    CancelAttacks();
                }

                StartDodgeProcess();

                bufferedAction = ActionType.None;
                bufferTimer = 0f;
            }
            else if (isDodging && currentDodgeCooldown <= 0f)
            {
                bufferedAction = ActionType.Dodge;
                bufferTimer = inputBufferTime;
            }
        }

        // ========================================================
        // ATAQUE
        // ========================================================

        if (WasPressed(attackAction))
        {
            if (isStationaryAttacking ||
                isMovingAttacking ||
                isDodging)
            {
                bufferedAction = ActionType.Attack;
                bufferTimer = inputBufferTime;
            }
            else
            {
                StartAttackProcess();
            }
        }

        // ========================================================
        // BLOQUEO DE MOVIMIENTO
        // ========================================================

        if (isStationaryAttacking ||
            isDodging ||
            isPickingUp)
        {
            return;
        }

        // ========================================================
        // MOVIMIENTO
        // ========================================================

        Vector2 moveInput = ReadMoveInput();

        float moveX = moveInput.x;
        float moveZ = moveInput.y;

        Vector3 moveDir =
            new Vector3(moveX, 0f, moveZ).normalized;

        if (moveDir.magnitude >= 0.1f)
        {
            bool isRunning = IsPressed(runAction);

            float targetSpeed =
                isRunning ? runSpeed : walkSpeed;

            Quaternion targetRotation =
                Quaternion.LookRotation(moveDir);

            transform.rotation =
                Quaternion.Slerp(
                    transform.rotation,
                    targetRotation,
                    rotationSpeed * Time.deltaTime
                );

            controller.Move(
                moveDir *
                targetSpeed *
                Time.deltaTime
            );

            animator.SetFloat(
                "Speed",
                isRunning ? 6f : 2f,
                0.1f,
                Time.deltaTime
            );

            if (isRunning &&
                rustDust != null &&
                !rustDust.isPlaying)
            {
                rustDust.Play();
            }
            else if (!isRunning &&
                     rustDust != null &&
                     rustDust.isPlaying)
            {
                rustDust.Stop();
            }
        }
        else
        {
            animator.SetFloat(
                "Speed",
                0f,
                0.1f,
                Time.deltaTime
            );

            if (rustDust != null &&
                rustDust.isPlaying)
            {
                rustDust.Stop();
            }
        }

        ApplyGravity();

        // ========================================================
        // COMBAT MODE
        // ========================================================

        if (isInCombat)
        {
            combatTimer -= Time.deltaTime;

            if (combatTimer <= 0f)
            {
                isInCombat = false;
                animator.SetBool("InCombat", false);
            }
        }
    }

    // ============================================================
    // INPUT HELPERS
    // ============================================================

    Vector2 ReadMoveInput()
    {
        if (moveAction == null ||
            moveAction.action == null)
        {
            return Vector2.zero;
        }

        return moveAction.action.ReadValue<Vector2>();
    }

    bool WasPressed(InputActionReference actionReference)
    {
        if (actionReference == null ||
            actionReference.action == null)
        {
            return false;
        }

        return actionReference.action.WasPressedThisFrame();
    }

    bool IsPressed(InputActionReference actionReference)
    {
        if (actionReference == null ||
            actionReference.action == null)
        {
            return false;
        }

        return actionReference.action.IsPressed();
    }

    void EnableInputAction(InputActionReference actionReference)
    {
        if (actionReference != null &&
            actionReference.action != null)
        {
            actionReference.action.Enable();
        }
    }

    void DisableInputAction(InputActionReference actionReference)
    {
        if (actionReference != null &&
            actionReference.action != null)
        {
            actionReference.action.Disable();
        }
    }

    // ============================================================
    // GRAVEDAD
    // ============================================================

    void ApplyGravity()
    {
        if (controller.isGrounded)
        {
            verticalVelocity = -0.5f;
        }
        else
        {
            verticalVelocity +=
                gravity * Time.deltaTime;
        }

        controller.Move(
            new Vector3(
                0,
                verticalVelocity,
                0
            ) * Time.deltaTime
        );
    }

    // ============================================================
    // MUERTE
    // ============================================================

    public void HandleDeath()
    {
        if (isDead)
            return;

        isDead = true;

        CancelAttacks();

        animator.SetTrigger("Die");
        animator.SetFloat("Speed", 0f);
        animator.SetBool("InCombat", false);

        if (rustDust != null &&
            rustDust.isPlaying)
        {
            rustDust.Stop();
        }
    }

    // ============================================================
    // COMBATE
    // ============================================================

    void ActivateCombatMode()
    {
        isInCombat = true;
        combatTimer = combatModeDuration;

        animator.SetBool("InCombat", true);
    }

    // ============================================================
    // TRAILS
    // ============================================================

    void SetTrails(bool state)
    {
        if (currentWeapon != WeaponType.Unarmed)
            return;

        foreach (TrailRenderer trail in punchTrails)
        {
            if (trail != null)
            {
                trail.emitting = state;
            }
        }
    }

    // ============================================================
    // HITBOXES
    // ============================================================

    void SetHitboxes(bool state)
    {
        if (currentWeapon == WeaponType.Pipe)
        {
            if (pipeHitbox != null)
            {
                if (state)
                    pipeHitbox.EnableHitbox();
                else
                    pipeHitbox.DisableHitbox();
            }
        }
        else if (currentWeapon == WeaponType.Unarmed)
        {
            if (state)
            {
                if (leftHandHitbox != null)
                    leftHandHitbox.EnableHitbox();

                if (rightHandHitbox != null)
                    rightHandHitbox.EnableHitbox();
            }
            else
            {
                if (leftHandHitbox != null)
                    leftHandHitbox.DisableHitbox();

                if (rightHandHitbox != null)
                    rightHandHitbox.DisableHitbox();
            }
        }
    }

    // ============================================================
    // DODGE
    // ============================================================

    void StartDodgeProcess()
    {
        currentDodgeCooldown = dodgeCooldown;

        Vector2 moveInput = ReadMoveInput();

        float x = moveInput.x;
        float z = moveInput.y;

        StartCoroutine(
            PerformDodge(x, z)
        );
    }

    // ============================================================
    // ATAQUE
    // ============================================================

    void StartAttackProcess()
    {
        ActivateCombatMode();

        Vector2 moveInput = ReadMoveInput();

        Vector3 moveDir =
            new Vector3(
                moveInput.x,
                0f,
                moveInput.y
            ).normalized;

        bool isMoving =
            moveDir.magnitude > 0.1f;

        if (isMoving)
        {
            attackCoroutine =
                StartCoroutine(
                    PerformMovingAttack()
                );
        }
        else
        {
            attackCoroutine =
                StartCoroutine(
                    PerformStationaryAttack()
                );
        }
    }

    void CancelAttacks()
    {
        if (attackCoroutine != null)
        {
            StopCoroutine(attackCoroutine);
            attackCoroutine = null;
        }

        if (lungeCoroutine != null)
        {
            StopCoroutine(lungeCoroutine);
            lungeCoroutine = null;
        }

        isStationaryAttacking = false;
        isMovingAttacking = false;

        animator.SetFloat("Speed", 0f);

        SetTrails(false);
        SetHitboxes(false);
    }

    // ============================================================
    // INPUT BUFFER
    // ============================================================

    void CheckBufferedAction()
    {
        if (bufferedAction != ActionType.None &&
            bufferTimer > 0f)
        {
            ActionType next =
                bufferedAction;

            bufferedAction =
                ActionType.None;

            bufferTimer = 0f;

            if (next == ActionType.Dodge &&
                currentDodgeCooldown <= 0f)
            {
                StartDodgeProcess();
            }
            else if (next == ActionType.Attack)
            {
                StartAttackProcess();
            }
        }
    }

    // ============================================================
    // AIM
    // ============================================================

    Vector3 GetMouseAimDirection()
    {
        if (Camera.main == null)
            return transform.forward;

        Vector2 mousePosition =
            Vector2.zero;

        if (mousePositionAction != null &&
            mousePositionAction.action != null)
        {
            mousePosition =
                mousePositionAction.action.ReadValue<Vector2>();
        }

        Ray ray =
            Camera.main.ScreenPointToRay(
                mousePosition
            );

        Plane groundPlane =
            new Plane(
                Vector3.up,
                transform.position
            );

        if (groundPlane.Raycast(
            ray,
            out float rayDistance))
        {
            Vector3 point =
                ray.GetPoint(rayDistance);

            Vector3 lookDirection =
                new Vector3(
                    point.x -
                    transform.position.x,

                    0f,

                    point.z -
                    transform.position.z
                );

            if (lookDirection != Vector3.zero)
            {
                transform.rotation =
                    Quaternion.LookRotation(
                        lookDirection
                    );

                return lookDirection.normalized;
            }
        }

        return transform.forward;
    }

    // ============================================================
    // ATAQUE ESTÁTICO
    // ============================================================

    IEnumerator PerformStationaryAttack()
    {
        if (currentWeapon == WeaponType.Revolver &&
            revolverController != null &&
            !revolverController.CanShoot())
        {
            yield break;
        }

        isStationaryAttacking = true;

        animator.SetTrigger("AttackSingle");

        SetTrails(true);
        SetHitboxes(true);

        float currentLungeSpeed =
            currentWeapon ==
            WeaponType.Revolver
                ? 0f
                : lungeSpeed;

        lungeCoroutine =
            StartCoroutine(
                AttackLungeRoutine(
                    currentLungeSpeed
                )
            );

        yield return lungeCoroutine;

        if (currentWeapon ==
            WeaponType.Revolver)
        {
            if (revolverController != null &&
                revolverController.CanShoot())
            {
                Vector3 aimDir =
                    GetMouseAimDirection();

                revolverController.Shoot(
                    false,
                    aimDir
                );
            }
        }

        SetTrails(false);
        SetHitboxes(false);

        float currentDuration;

        if (currentWeapon ==
            WeaponType.Revolver)
        {
            currentDuration =
                shootDuration;
        }
        else if (currentWeapon ==
                 WeaponType.Pipe)
        {
            currentDuration =
                weaponAttackDuration;
        }
        else
        {
            currentDuration =
                fistAttackDuration;
        }

        yield return new WaitForSeconds(
            currentDuration -
            lungeDuration
        );

        isStationaryAttacking = false;

        CheckBufferedAction();
    }

    // ============================================================
    // ATAQUE EN MOVIMIENTO
    // ============================================================

    IEnumerator PerformMovingAttack()
    {
        isMovingAttacking = true;

        animator.SetTrigger("AttackCombo");

        SetTrails(true);
        SetHitboxes(true);

        float currentComboTime;

        if (currentWeapon ==
            WeaponType.Revolver)
        {
            currentComboTime =
                shootComboDuration;
        }
        else if (currentWeapon ==
                 WeaponType.Pipe)
        {
            currentComboTime =
                weaponComboDuration;
        }
        else
        {
            currentComboTime =
                fistComboDuration;
        }

        yield return new WaitForSeconds(
            currentComboTime * 0.5f
        );

        if (currentWeapon ==
            WeaponType.Revolver &&
            revolverController != null &&
            revolverController.CanShoot())
        {
            Vector3 aimDir =
                GetMouseAimDirection();

            revolverController.Shoot(
                true,
                aimDir
            );
        }

        yield return new WaitForSeconds(
            currentComboTime * 0.1f
        );

        SetTrails(false);
        SetHitboxes(false);

        yield return new WaitForSeconds(
            currentComboTime * 0.4f
        );

        bufferedAction =
            ActionType.None;

        bufferTimer = 0f;

        isMovingAttacking = false;

        CheckBufferedAction();
    }

    // ============================================================
    // LUNGE
    // ============================================================

    IEnumerator AttackLungeRoutine(float speed)
    {
        float timer = 0f;

        while (timer < lungeDuration)
        {
            Vector3 lungeDir =
                transform.forward * speed;

            lungeDir.y =
                verticalVelocity;

            if (isStationaryAttacking)
            {
                controller.Move(
                    lungeDir *
                    Time.deltaTime
                );
            }

            animator.SetFloat(
                "Speed",
                6f
            );

            timer += Time.deltaTime;

            yield return null;
        }

        animator.SetFloat(
            "Speed",
            0f
        );
    }

    // ============================================================
    // DODGE
    // ============================================================

    IEnumerator PerformDodge(
        float x,
        float z)
    {
        isDodging = true;

        ActivateCombatMode();

        if (rustDust != null)
        {
            rustDust.Emit(30);
        }

        string triggerName =
            "DodgeForward";

        if (z < -0.5f)
        {
            triggerName =
                "DodgeBack";
        }
        else if (x > 0.5f)
        {
            triggerName =
                "DodgeRight";
        }
        else if (x < -0.5f)
        {
            triggerName =
                "DodgeLeft";
        }

        animator.SetTrigger(
            triggerName
        );

        if (iFrameStartDelay > 0)
        {
            yield return new WaitForSeconds(
                iFrameStartDelay
            );
        }

        isInvulnerable = true;

        Coroutine blinkCoroutine =
            StartCoroutine(
                BlinkEffect()
            );

        yield return new WaitForSeconds(
            iFrameDuration
        );

        if (blinkCoroutine != null)
        {
            StopCoroutine(
                blinkCoroutine
            );
        }

        SetRenderersVisible(true);

        isInvulnerable = false;

        float remainingTime =
            dodgeTotalDuration -
            (iFrameStartDelay +
             iFrameDuration);

        if (remainingTime > 0)
        {
            yield return new WaitForSeconds(
                remainingTime
            );
        }

        isDodging = false;

        CheckBufferedAction();
    }

    // ============================================================
    // MERCY I-FRAMES
    // ============================================================

    public void TriggerMercyIFrames()
    {
        if (gameObject.activeInHierarchy &&
            !isDead)
        {
            StartCoroutine(
                MercyIFrameRoutine()
            );
        }
    }

    IEnumerator MercyIFrameRoutine()
    {
        isInvulnerable = true;

        Coroutine blink =
            StartCoroutine(
                BlinkEffect()
            );

        yield return new WaitForSeconds(
            mercyInvincibilityDuration
        );

        if (blink != null)
        {
            StopCoroutine(blink);
        }

        SetRenderersVisible(true);

        if (!isDodging)
        {
            isInvulnerable = false;
        }
    }

    // ============================================================
    // BLINK
    // ============================================================

    IEnumerator BlinkEffect()
    {
        bool isVisible = true;

        while (true)
        {
            isVisible =
                !isVisible;

            SetRenderersVisible(
                isVisible
            );

            yield return new WaitForSeconds(
                flashInterval
            );
        }
    }

    void SetRenderersVisible(bool state)
    {
        foreach (Renderer rend in characterRenderers)
        {
            if (rend != null)
            {
                rend.enabled = state;
            }
        }
    }

    // ============================================================
    // ARMAS
    // ============================================================

    void ChangeWeapon(
        WeaponType newWeapon)
    {
        if (currentWeapon ==
            newWeapon)
        {
            return;
        }

        currentWeapon =
            newWeapon;

        UpdateWeaponVisibility();

        SetTrails(false);
        SetHitboxes(false);
    }

    void UpdateWeaponVisibility()
    {
        if (pipeModel != null)
        {
            pipeModel.SetActive(
                currentWeapon ==
                WeaponType.Pipe
            );
        }

        if (revolverModel != null)
        {
            revolverModel.SetActive(
                currentWeapon ==
                WeaponType.Revolver
            );
        }
    }

    // ============================================================
    // PICKUP
    // ============================================================

    public void EquipWeapon(
        WeaponType droppedWeapon)
    {
        if (droppedWeapon ==
            WeaponType.Pipe)
        {
            hasPipe = true;
        }

        if (droppedWeapon ==
            WeaponType.Revolver)
        {
            hasRevolver = true;
        }

        StartCoroutine(
            PickupRoutine(
                droppedWeapon
            )
        );
    }

    IEnumerator PickupRoutine(
        WeaponType droppedWeapon)
    {
        isPickingUp = true;

        animator.SetTrigger(
            "Pickup"
        );

        yield return new WaitForSeconds(
            0.5f
        );

        ChangeWeapon(
            droppedWeapon
        );

        yield return new WaitForSeconds(
            1.5f
        );

        isPickingUp = false;
    }

    // ============================================================
    // CICLAR ARMAS
    // ============================================================

    void CycleWeapon(
        int direction)
    {
        int currentIndex =
            (int)currentWeapon;

        int totalWeapons = 3;

        for (int i = 0;
             i < totalWeapons;
             i++)
        {
            currentIndex +=
                direction;

            if (currentIndex >=
                totalWeapons)
            {
                currentIndex = 0;
            }

            if (currentIndex < 0)
            {
                currentIndex =
                    totalWeapons - 1;
            }

            WeaponType nextWeapon =
                (WeaponType)currentIndex;

            if (nextWeapon ==
                WeaponType.Unarmed)
            {
                ChangeWeapon(
                    nextWeapon
                );

                return;
            }
            else if (
                nextWeapon ==
                WeaponType.Pipe &&
                hasPipe)
            {
                ChangeWeapon(
                    nextWeapon
                );

                return;
            }
            else if (
                nextWeapon ==
                WeaponType.Revolver &&
                hasRevolver)
            {
                ChangeWeapon(
                    nextWeapon
                );

                return;
            }
        }
    }
}