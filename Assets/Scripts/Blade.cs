using UnityEngine;

public enum BladeState { Idle, Slicing }

public class Blade : VanMonoBeha
{

    public BladeState State { get; private set; } = BladeState.Idle;
    Vector3 lastPosition;
    Collider bladeCollider;
    SphereCollider bladeSphere;
    Camera mainCam;
    TrailRenderer bladeTrail;
    // shortcut: fixed 128-hit buffer; expand if one swipe can overlap more colliders.
    readonly Collider[] sliceHits = new Collider[128];

    public Vector3 direction { get; private set; }
    [SerializeField] float minSliceVelocity = 0.01f;
    public float sliceForce = 5f;

    public void SetSensitivity(float value)
    {
        minSliceVelocity = Mathf.Lerp(0.02f, 0f, Mathf.Clamp01(value));
        sliceForce = Mathf.Lerp(2f, 8f, Mathf.Clamp01(value));
    }

    protected override void Awake()
    {
        base.Awake();
       this.LoadCompnents();
    }
    protected override void LoadCompnents()
    {
        base.LoadCompnents();
        mainCam = Camera.main;
        this.bladeCollider = GetComponent<Collider>();
        this.bladeSphere = bladeCollider as SphereCollider;
        this.bladeTrail = GetComponentInChildren<TrailRenderer>();
    }

    private void Update()
    {
        this.HandleMouse();
    }
    void HandleMouse()
    {
        if (GameManager.instance == null || !GameManager.instance.IsGameRunning || GameManager.instance.IsPaused)
        {
            StopSlicing();
            return;
        }
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            if (touch.phase == TouchPhase.Began) StartSlicing(touch.position);
            else if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled) StopSlicing();
            else if (State == BladeState.Slicing) ContinueSlicing(touch.position);
            return;
        }
        if (Input.GetMouseButtonDown(0))
        {
            StartSlicing(Input.mousePosition);
        }
        else if (Input.GetMouseButtonUp(0))
        {
            this.StopSlicing();
        }
        else if (State == BladeState.Slicing)
        {
            this.ContinueSlicing(Input.mousePosition);
        }
    }
    private void OnDisable()
    {
        StopAllCoroutines();
        StopSlicing();
    }
    private void OnEnable()
    {
        StopSlicing();
    }
    void StartSlicing(Vector2 screenPosition)
    {
        if (State != BladeState.Idle || mainCam == null || bladeCollider == null || bladeTrail == null) return;
        Vector3 newPosion = mainCam.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, 0f));
        newPosion.z = 0;
        transform.position = newPosion;
        lastPosition = newPosion;

        State = BladeState.Slicing;
        bladeCollider.enabled = true;

        if (FruitNinjaAudio.Instance != null) FruitNinjaAudio.Instance.PlayWhoosh();

        this.bladeTrail.enabled = true;
        this.bladeTrail.Clear();
    }
    void StopSlicing()
    {
        State = BladeState.Idle;
        if (bladeCollider != null) bladeCollider.enabled = false;
        if (bladeTrail != null) bladeTrail.enabled = false;


    }
    void ContinueSlicing(Vector2 screenPosition)
    {
        if (State != BladeState.Slicing) return;
        Vector3 previousPosition = transform.position;
        lastPosition = previousPosition;
        Vector3 newPosion = mainCam.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, 0f));
        newPosion.z = 0;
        direction = newPosion - previousPosition;
        float velocity = direction.magnitude / Time.deltaTime;
        bladeCollider.enabled = velocity > minSliceVelocity;

        transform.position = newPosion;
        if (bladeCollider.enabled) SweepSlice(previousPosition, newPosion);
    }

    void SweepSlice(Vector3 from, Vector3 to)
    {
        Vector3 scaleVector = transform.lossyScale;
        float scale = Mathf.Max(scaleVector.x, Mathf.Max(scaleVector.y, scaleVector.z));
        float radius = bladeSphere != null ? bladeSphere.radius * scale : bladeCollider.bounds.extents.magnitude;
        int count = Physics.OverlapCapsuleNonAlloc(from, to, radius, sliceHits, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Collide);

        for (int i = 0; i < count; i++)
            if (sliceHits[i] != null && sliceHits[i].GetComponentInParent<Bomb>() != null) return;

        for (int i = 0; i < count; i++)
        {
            Fruit fruit = sliceHits[i] != null ? sliceHits[i].GetComponentInParent<Fruit>() : null;
            if (fruit != null) fruit.TrySlice(direction, transform.position, sliceForce);
        }
    }

    public bool IsPathClear()
    {
        Vector3 scaleVector = transform.lossyScale;
        float scale = Mathf.Max(scaleVector.x, Mathf.Max(scaleVector.y, scaleVector.z));
        float radius = bladeSphere != null ? bladeSphere.radius * scale : bladeCollider.bounds.extents.magnitude;
        int count = Physics.OverlapCapsuleNonAlloc(lastPosition, transform.position, radius, sliceHits, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Collide);
        for (int i = 0; i < count; i++)
            if (sliceHits[i] != null && sliceHits[i].GetComponentInParent<Bomb>() != null) return false;
        return true;
    }
   
}
