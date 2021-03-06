
using UnityEngine;


public class PlayerController : MonoBehaviour
{
    // Declare static, references, variables
    public static PlayerController instance { get; private set; }
    private static float MAX_CAM_REACH = 100.0f;

    [Header("References")]
    [SerializeField] private UIController uiController;
    [SerializeField] private Construct playerConstruct;
    [SerializeField] private Transform camPivot;
    [SerializeField] private Transform camOrbit;
    [SerializeField] private Camera _cam;
    public Camera cam => _cam;

    [Header("Config")]
    [SerializeField]
    private StatList camStats = new StatList()
    {
        ["rotateSpeed"] = 0.5f,
        ["followSpeed"] = 15.0f,
        ["offsetSpeed"] = 6.0f,
        ["zoomSpeed"] = 4.0f,
        ["clipDistance"] = 0.2f
    };

    private IngameState ingameState;
    private ForgingState forgingState;
    private State state;
    public WorldObject prevAimedWO { get; private set; }
    public WorldObject aimedWO { get; private set; }
    public Vector3 prevAimedPos { get; private set; }
    public Vector3 aimedPos { get; private set; }


    private void Awake()
    {
        // Handle singleton
        if (instance != null) return;
        instance = this;

        // Initialize state
        ingameState = new IngameState(this);
        forgingState = new ForgingState(this);
    }


    private void Start()
    {
        // Initialize variables
        SetState(ingameState);
    }


    private void Update()
    {
        // Run updates
        UpdateAimed();
        state.Update();
    }

    private void UpdateAimed()
    {
        // Raycast out from the camera
        prevAimedPos = aimedPos;
        prevAimedWO = aimedWO;
        Ray ray = cam.ScreenPointToRay(Input.mousePosition);

        // Hit some object
        if (Physics.Raycast(ray, out RaycastHit hit, MAX_CAM_REACH))
        {
            aimedPos = hit.point;
            aimedWO = hit.transform.GetComponent<WorldObject>();
        }

        // Did not hit anything
        else
        {
            aimedPos = ray.GetPoint(MAX_CAM_REACH);
            aimedWO = null;
        }
    }


    private void FixedUpdate()
    {
        // Run fixed updates
        state.FixedUpdate();
    }


    private void LateUpdate()
    {
        // Run late updates
        state.LateUpdate();
    }


    public Transform GetPivot() => camPivot;


    private void SetState(State state_)
    {
        // Update state to new state
        if (state == state_) return;
        if (state != null) state.Unset();
        state = state_;
        state.Set();
    }


    public abstract class State
    {
        protected PlayerController pcn;

        public State(PlayerController pcn_) { pcn = pcn_; }

        public virtual void Set() { }
        public virtual void Unset() { }

        public virtual void Update() { }
        public virtual void FixedUpdate() { }
        public virtual void LateUpdate() { }
    }

    private class IngameState : State
    {
        // Declare static, variables
        private static float[] ZOOM_RANGE = new float[] { -35f, -3.5f };

        private Vector3 inputMoveDir;
        private ConstructObject currentCO;
        private float[] zoomRange;
        private Vector3 camOffset;
        private float lastPivotUpdateTime;


        public IngameState(PlayerController pcn_) : base(pcn_) { }


        public override void Set()
        {
            // Initialize variables
            pcn.uiController.SetIngame(true);
            Cursor.lockState = CursorLockMode.Locked;
            currentCO = pcn.playerConstruct.GetCentreCO();
            ResetOffset();
        }

        public override void Unset()
        {
            // Update variables
            pcn.uiController.SetIngame(false);
            Cursor.lockState = CursorLockMode.None;
        }


        public override void Update()
        {
            // Run updates
            UpdateConstruct();
            UpdateCamera();

            // Handle changing state
            if (Input.GetKeyDown("tab") && pcn.playerConstruct.GetCanForge()) pcn.SetState(pcn.forgingState);
        }

        private void UpdateConstruct()
        {
            // Input movement with "Horizontal" and "Vertical"
            inputMoveDir = Vector3.zero;
            if (Input.GetAxisRaw("Horizontal") != 0.0f || Input.GetAxisRaw("Vertical") != 0.0f)
            {
                Transform camPivot = pcn.GetPivot();
                Vector3 flatForward = Vector3.ProjectOnPlane(camPivot.forward, Vector3.up).normalized;
                inputMoveDir += camPivot.right * Input.GetAxisRaw("Horizontal");
                inputMoveDir += flatForward * Input.GetAxisRaw("Vertical");
            }

            // Send ability skill controls (hardcode: f)
            if (Input.GetKeyDown("f")) pcn.playerConstruct.skills.Use("f");
            foreach (string key in pcn.playerConstruct.abilityButtons)
            {
                if (key.StartsWith("_"))
                {
                    if (Input.GetMouseButtonDown(key[1] - '0')) pcn.playerConstruct.skills.Use(key);
                }
                else if (Input.GetKeyDown(key)) pcn.playerConstruct.skills.Use(key);
            }
        }

        private void UpdateCamera()
        {
            // Update current CO and offset
            ConstructObject newCO = pcn.playerConstruct.GetCentreCO();
            if (currentCO != newCO)
            {
                currentCO = newCO;
                ResetOffset();
            }

            // Update pivot position and rotation
            pcn.camPivot.Rotate(0, Input.GetAxis("Mouse X") * pcn.camStats["rotateSpeed"], 0, Space.World);
            pcn.camPivot.Rotate(-Input.GetAxis("Mouse Y") * pcn.camStats["rotateSpeed"], 0, 0, Space.Self);

            // Zoom in / out offset z based on scroll wheel
            float zoom = Input.GetAxis("Mouse ScrollWheel");
            if (zoom != 0.0f) camOffset.z = Mathf.Clamp(camOffset.z + zoom * pcn.camStats["zoomSpeed"], zoomRange[0], zoomRange[1]);

            // Raycast out towards target offset position to prevent clipping
            Vector3 rayDir = pcn.camPivot.rotation * camOffset;
            LayerMask layer = LayerMask.GetMask("Environment");
            if (Physics.Raycast(pcn.camPivot.position, rayDir, out RaycastHit hit, camOffset.magnitude, layer))
            {
                Vector3 clampedDir = (hit.point - pcn.camPivot.position);
                Vector3 clampedPos = pcn.camPivot.position + clampedDir + clampedDir.normalized * -pcn.camStats["clipDistance"];
                pcn.camOrbit.position = clampedPos;
            }

            // No clipping so lerp offset towards target offset
            else pcn.camOrbit.localPosition = Vector3.Lerp(pcn.camOrbit.localPosition, camOffset, pcn.camStats["offsetSpeed"] * Time.deltaTime);
        }


        public override void FixedUpdate()
        {
            // Run fixed updates
            FixedUpdateConstruct();
            FixedUpdateCamera();
        }

        private void FixedUpdateConstruct() {
            // Aim and move construct
            if (!pcn.playerConstruct.GetContainsWO(pcn.aimedWO)) pcn.playerConstruct.AimAtPosition(pcn.aimedPos);
            if (inputMoveDir != Vector3.zero) pcn.playerConstruct.MoveInDirection(inputMoveDir);
        }

        private void FixedUpdateCamera() => UpdatePivotPosition();


        private void UpdatePivotPosition()
        {
            // Calculate delta time and update position
            float deltaTime = Time.time - lastPivotUpdateTime;
            lastPivotUpdateTime = Time.time;
            pcn.camPivot.position = Vector3.Lerp(pcn.camPivot.position, currentCO.transform.position, pcn.camStats["followSpeed"] * deltaTime);
        }


        private void ResetOffset()
        {
            // Update zoom range and target offset based on current CO max extent
            float maxExtent = currentCO.baseWO.GetMaxExtent();
            zoomRange = new float[] { maxExtent * ZOOM_RANGE[0], maxExtent * ZOOM_RANGE[1] };
            camOffset = new Vector3(
                Mathf.Max(0.25f, maxExtent + 0.2f),
                Mathf.Max(0.48f, maxExtent + 0.25f),
                Mathf.Clamp(pcn.camOrbit.localPosition.z, zoomRange[0], zoomRange[1])
            );
        }
    }

    private class ForgingState : State
    {
        // Declare static, variables
        private static float[] ZOOM_RANGE = new float[] { -35f, -3.5f };

        private float inputZoom;
        ConstructObject currentCO;
        private float[] zoomRange;


        public ForgingState(PlayerController pcn_) : base(pcn_) { }


        public override void Set()
        {
            // Update construct and offset
            pcn.uiController.SetForging(true);
            pcn.playerConstruct.SetForging(true);
            currentCO = pcn.playerConstruct.GetCentreCO();
            ResetOffset();
        }

        public override void Unset()
        {
            // Update construct
            pcn.uiController.SetForging(false);
            pcn.playerConstruct.SetForging(false);
        }


        public override void Update()
        {
            // Run updates
            UpdateCamera();

            // Handle changing state
            if (Input.GetKeyDown("tab")) pcn.SetState(pcn.ingameState);
        }

        private void UpdateCamera()
        {
            // Zoom in / out based on scroll wheel
            inputZoom += Input.GetAxis("Mouse ScrollWheel");
            if (inputZoom != 0)
            {
                Vector3 local = pcn.camOrbit.transform.localPosition;
                float change = inputZoom * pcn.camStats["zoomSpeed"] * Time.deltaTime;
                local.z += change;
                inputZoom -= change;
                pcn.camOrbit.localPosition = local;
            }

            // Move camera towards centre object
            pcn.camPivot.position = Vector3.Lerp(pcn.camPivot.position, currentCO.transform.position, pcn.camStats["followSpeed"] * Time.deltaTime);
            pcn.camOrbit.localPosition = new Vector3(
                Mathf.Lerp(pcn.camOrbit.localPosition.x, 0.0f, pcn.camStats["offsetSpeed"] * Time.deltaTime),
                Mathf.Lerp(pcn.camOrbit.localPosition.y, 0.0f, pcn.camStats["offsetSpeed"] * Time.deltaTime),
                Mathf.Clamp(pcn.camOrbit.localPosition.z, zoomRange[0], zoomRange[1])
            );
        }


        private void ResetOffset()
        {
            // Update max extent and max extent based on construct
            float maxExtent = currentCO.baseWO.GetMaxExtent();
            zoomRange = new float[] { maxExtent * ZOOM_RANGE[0], maxExtent * ZOOM_RANGE[1] };
            pcn.camOrbit.localPosition = new Vector3(
                pcn.camOrbit.localPosition.x,
                pcn.camOrbit.localPosition.y,
                Mathf.Clamp(pcn.camOrbit.localPosition.z, zoomRange[0], zoomRange[1])
            );
        }
    }
}
