using System.Collections.Generic;
using UnityEngine;

public class OrganicGazeMap : MonoBehaviour
{
    [Header("Gaze Source")]
    public Camera headCamera;

    [Header("Projection")]
    public bool followCameraPosition = true;
    public Vector3 fixedCentre = Vector3.zero;

    [Header("Projection radius based on look count")]
    public float radiusAtStart = 3f; public float radiusAtMax = 14f; public int looksForMaxRadius = 30;
    [Range(0.1f, 5f)] public float radiusEaseSpeed = 0.6f;
    private float projectionRadius; 

    [Header("Smoothing")]
    [Tooltip("lag")]
    [Range(0.01f, 1f)] public float followDamping = 0.04f;
    [Tooltip("no. points  between sample coords")]
    [Range(2, 12)] public int splineResolution = 7;
    [Tooltip("min travel before new point is recorded")]
    public float minSampleDistance = 0.12f;

    [Header("Width based on movement speed")]
    public float widthSlow = 0.4f;        // thick when gaze is slow
    public float widthFast = 0.04f;        // thin when head whips
    public float speedForThinnest = 4f;
    [Tooltip("how fast width transitions toward the new width target")]
    [Range(0.5f, 10f)] public float widthEaseSpeed = 2f;

    [Header("Fixations")]
    public GameObject fixationNodePrefab;          // small blob/sphere
    [Tooltip("min speed to count as a fixation hold.")]
    public float fixationSpeedThreshold = 0.4f;
    [Tooltip("Seconds of dwell before the node blooms.")]
    public float fixationDwellTime = 0.4f;
    [Tooltip("minimum time gap between spawned nodes ")]
    public float fixationCooldown = 0.8f;
    // public float nodeMaxScale = 0.9f;
    // public float nodeGrowTime = 0.7f;

    [Header("Appearance")]
    public Material lineMaterial;
    public Gradient lineColor;     

    // where the gaze is at
    private Vector3 currentGazeDir = Vector3.forward;
    private Vector3 smoothedPoint;
    private Vector3 lastSamplePoint;
    private Vector3 prevFramePoint;
    private bool initialised = false;
    private bool usingNetworkGaze = false;

    // speed n width
    private float currentSpeed = 0f;
    private float smoothedWidth;

    // fixations
    private FixationNode activeNode = null;
    private float dwellTimer = 0f;
    private float cooldownTimer = 0f;

    // points for the smooth line spline
    private List<Vector3> controlPoints = new List<Vector3>();

    // mesh ribbon:
    private List<Vector3> ribbonVerts = new List<Vector3>();
    private List<int> ribbonTris = new List<int>();
    private List<Color> ribbonColors = new List<Color>();
    private Mesh ribbonMesh;
    private MeshFilter meshFilter;
    private Vector3 lastRibbonPoint;
    private Vector3 lastRibbonNormal = Vector3.up;
    private bool ribbonStarted = false;

    void Awake()
    {
        smoothedWidth = widthSlow;
        projectionRadius = radiusAtStart;

        // building the mesh ribbon renderer for the fluid line:
        meshFilter = gameObject.AddComponent<MeshFilter>();
        MeshRenderer mr = gameObject.AddComponent<MeshRenderer>();
        mr.material = lineMaterial;
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        mr.receiveShadows = false;

        ribbonMesh = new Mesh();
        ribbonMesh.name = "GazeRibbon";
        ribbonMesh.MarkDynamic();
        meshFilter.mesh = ribbonMesh;
    }

    void Update()
    {
        UpdateProjectionRadius(); UpdateGaze();

        Vector3 target = ProjectGazePoint();
        if (!initialised)
        {
            smoothedPoint = target;
            prevFramePoint = target;
            lastSamplePoint = target;
            lastRibbonPoint = target;
            initialised = true;
            controlPoints.Add(target);
            return;
        }

        // lag for smoothness
        float damp = 1f - Mathf.Pow(1f - followDamping, Time.deltaTime * 60f);
        smoothedPoint = Vector3.Lerp(smoothedPoint, target, damp);

        // speed measurement
        currentSpeed = Vector3.Distance(smoothedPoint, prevFramePoint) / Time.deltaTime;
        prevFramePoint = smoothedPoint;

        float speedT = Mathf.Clamp01(currentSpeed / speedForThinnest);
        float targetWidth = Mathf.Lerp(widthSlow, widthFast, speedT);
        smoothedWidth = Mathf.Lerp(smoothedWidth, targetWidth,
            Time.deltaTime * widthEaseSpeed);

        // only take points if the headset has moved enough (min sample distance)
        if (Vector3.Distance(smoothedPoint, lastSamplePoint) >= minSampleDistance)
        {
            controlPoints.Add(smoothedPoint);
            lastSamplePoint = smoothedPoint;

            // draw the new segment as a ribbon
            ExtendRibbon();
        }

        // detect focus moments
        HandleFixation();
    }

    void UpdateProjectionRadius()
{
    int looks = 0;
    if (StimulusManager.Instance != null)
        looks = StimulusManager.Instance.LookCount;

    // mapping the looks onto the size of the projection sphere
    float t = looksForMaxRadius > 0 ? Mathf.Clamp01(looks / (float)looksForMaxRadius) : 0f;
    float targetRadius = Mathf.Lerp(radiusAtStart, radiusAtMax, t); // lerp to radius

    // Smoothly ease toward the target — never jumps
    projectionRadius = Mathf.Lerp(projectionRadius, targetRadius, Time.deltaTime * radiusEaseSpeed); // smooth lerp between size changes
}

    void UpdateGaze()
    {
        if (!usingNetworkGaze && headCamera != null)
            currentGazeDir = headCamera.transform.forward;
    }

    public void SetNetworkGaze(Vector3 worldGazeDirection)
    {
        usingNetworkGaze = true;
        currentGazeDir = worldGazeDirection.normalized;
    }

    Vector3 ProjectGazePoint()
    {
        Vector3 centre = followCameraPosition && headCamera != null
            ? headCamera.transform.position
            : fixedCentre;
        return centre + currentGazeDir.normalized * projectionRadius;
    }


    void ExtendRibbon()
    {
        int n = controlPoints.Count;
        if (n < 4) return;   // 4 points for the cr segment

        Vector3 p0 = controlPoints[n - 4];
        Vector3 p1 = controlPoints[n - 3];
        Vector3 p2 = controlPoints[n - 2];
        Vector3 p3 = controlPoints[n - 1];

        for (int i = 1; i <= splineResolution; i++)
        {
            float t = i / (float)splineResolution;
            Vector3 pos = CatmullRom(p0, p1, p2, p3, t);
            AddRibbonPoint(pos, smoothedWidth);
        }
    }

    void AddRibbonPoint(Vector3 pos, float width)
    {
        if (!ribbonStarted)
        {
            lastRibbonPoint = pos;
            ribbonStarted = true;
            return;
        }

        // direction
        Vector3 dir = (pos - lastRibbonPoint);
        if (dir.sqrMagnitude < 1e-6f) return;
        dir.Normalize();


        Vector3 viewDir = (Camera.main != null) ? (pos - Camera.main.transform.position).normalized : Vector3.forward;
        Vector3 side = Vector3.Cross(dir, viewDir).normalized;
        if (side.sqrMagnitude < 1e-6f) side = lastRibbonNormal;
        lastRibbonNormal = side;

        float halfW = width * 0.5f;

        // Two verts at the previous point and two at this point
        int baseIndex = ribbonVerts.Count;

        Vector3 prevSide = side * halfW;
        Vector3 curSide = side * halfW;

        ribbonVerts.Add(lastRibbonPoint - prevSide);
        ribbonVerts.Add(lastRibbonPoint + prevSide);
        ribbonVerts.Add(pos - curSide);
        ribbonVerts.Add(pos + curSide);

        Color c = lineColor != null ? lineColor.Evaluate(0.5f) : Color.white;
        ribbonColors.Add(c); ribbonColors.Add(c);
        ribbonColors.Add(c); ribbonColors.Add(c);

        ribbonTris.Add(baseIndex + 0);
        ribbonTris.Add(baseIndex + 2);
        ribbonTris.Add(baseIndex + 1);
        ribbonTris.Add(baseIndex + 1);
        ribbonTris.Add(baseIndex + 2);
        ribbonTris.Add(baseIndex + 3);

        lastRibbonPoint = pos;

        // add to mesh (incremental)
        ribbonMesh.SetVertices(ribbonVerts);
        ribbonMesh.SetColors(ribbonColors);
        ribbonMesh.SetTriangles(ribbonTris, 0);
        ribbonMesh.RecalculateBounds();
    }

    static Vector3 CatmullRom(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
    {
        float t2 = t * t;
        float t3 = t2 * t;
        return 0.5f * (
            (2f * p1) +
            (-p0 + p2) * t +
            (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 +
            (-p0 + 3f * p1 - 3f * p2 + p3) * t3
        );
    }


    void HandleFixation()
{
    cooldownTimer -= Time.deltaTime;

    if (currentSpeed < fixationSpeedThreshold)
    {
        dwellTimer += Time.deltaTime;

        if (dwellTimer >= fixationDwellTime && activeNode == null && cooldownTimer <= 0f)
        {
            SpawnNode(smoothedPoint);
        }

        // keeps growing the active node so it grows the longer they stare
        if (activeNode != null)
            activeNode.Expand(Time.deltaTime);
    }
    else
    {
        // stops growing the current node when gaze moves and begine the cooloff period between a new fixation and this one
        if (activeNode != null)
        {
            activeNode.StopGrowing();
            activeNode = null;
            cooldownTimer = fixationCooldown;
        }
        dwellTimer = 0f;
    }
}

    void SpawnNode(Vector3 pos)
    {
        if (fixationNodePrefab == null) return;

        GameObject node = Instantiate(fixationNodePrefab, pos, Quaternion.identity, transform);
        node.layer = gameObject.layer;
        
        activeNode = node.GetComponent<FixationNode>();
        if (activeNode != null)
            activeNode.Seed(pos);
    }

    public void ResetMap()
    {
        controlPoints.Clear();
        ribbonVerts.Clear();
        ribbonTris.Clear();
        ribbonColors.Clear();
        ribbonMesh.Clear();

        activeNode = null;

        ribbonStarted = false;
        initialised = false;

        // Remove spawned nodes
        foreach (Transform child in transform)
            Destroy(child.gameObject);
    }
}