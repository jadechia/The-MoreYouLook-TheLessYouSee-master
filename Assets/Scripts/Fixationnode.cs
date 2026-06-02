using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// *SCRIPT TO TURN MY FIXATION POINTS INTO BRANCHES ON MY ATTENTION MAP*
public class FixationNode : MonoBehaviour
{
    [Header("Objects")]
    public Material branchMaterial;
    public GameObject blobPrefab;

    [Header("Growth pace")]
    public float secondsPerGeneration = 0.3f;
    public int maxGenerations = 4;

    [Header("Branch shape")]
    public Vector2Int branchesPerBlob = new Vector2Int(1, 3); // every blob either spawns 1,2 or 3 new branches.
    public float baseBranchLength = 1.3f;
    [Tooltip("decreasing the size each round by this fraction (it's multiply not sub)")]
    public float lengthFalloff = 0.7f;
    public float baseBranchWidth = 0.06f;
    public float widthFalloff = 0.7f;
    public float curl = 0.4f;
    public float spreadAngle = 70f; // the random range 4 the spread of the branches as degrees
    public int branchResolution = 4;

    [Header("Blob Size")]
    public float baseBlobScale = 0.38f;
    public float blobFalloff = 0.75f;
    public float blobGrowTime = 0.4f;

    // private vars to track the internal growth state:
    private int totalBranches = 0;
    public int absoluteMaxBranches = 30;   // just in case of glitches i'm making a branch ceiling at 24 per node to avoid performance crashes.
    private int currentGeneration = 0;
    private float dwellAccumulated = 0f;
    private bool growing = true;

    private struct Tip // the tips are what sprouts the branches
    {
        public Vector3 pos;
        public Vector3 dir;
        public int gen;
    }
    private List<Tip> activeTips = new List<Tip>();

    public void Seed(Vector3 worldPos)
    {
        transform.position = worldPos;
        SpawnBlob(worldPos, 0); // spawn at the centre

        // growing the initial branch tips outward in random directions
        int count = Random.Range(branchesPerBlob.x, branchesPerBlob.y + 1);
        for (int i = 0; i < count; i++)
        {
            activeTips.Add(new Tip
            { pos = worldPos, dir = Random.onUnitSphere, gen = 0});
        }
    }


    public void Expand(float deltaTime) // growth animation
    {
        if (!growing) return;
        dwellAccumulated += deltaTime;

        if (dwellAccumulated >= secondsPerGeneration)
        {
            dwellAccumulated -= secondsPerGeneration;
            GrowGeneration();
        }
    }

    public void StopGrowing() {growing = false;} // stops the growth of a node, calling it when the gaze moves on

    void GrowGeneration() // func to halt the next generation of node branches if it passes max, performance ceiling
    {
        if (currentGeneration >= maxGenerations) {
            growing = false;
            return;
        }
        currentGeneration++;
        List<Tip> nextTips = new List<Tip>();

        foreach (Tip tip in activeTips) {

            if (totalBranches >= absoluteMaxBranches) { // checking if there are more than 60 brnaches in a node 
                growing = false; break;} 

            totalBranches++;
        
            int gen = tip.gen;
            float length = baseBranchLength * Mathf.Pow(lengthFalloff, gen);
            float width  = baseBranchWidth  * Mathf.Pow(widthFalloff, gen);

            // allows for the curved branch:
            Vector3 endPos = GrowBranch(tip.pos, tip.dir, length, width, gen);
            
            float blobScale = baseBlobScale * Mathf.Pow(blobFalloff, gen + 1);
            SpawnBlob(endPos, gen + 1, blobScale); // spawn a blob at the new tip

            // sprout new tips from the end of that blob in their given spread directions
            int count = Random.Range(branchesPerBlob.x, branchesPerBlob.y + 1);
            for (int i = 0; i < count; i++)
            {
                Vector3 newDir = RandomConeDirection(tip.dir, spreadAngle);
                nextTips.Add(new Tip { pos = endPos, dir = newDir, gen = gen + 1 });
            }
        }

        activeTips = nextTips;
        const int maxTips = 20;
        if (activeTips.Count > maxTips)
            activeTips.RemoveRange(maxTips, activeTips.Count - maxTips); // adding a fallback for performance bc the program kept crashing when the tip count exploded due to exponential branches.
    }
    // the function that grows the branches out from the blobs:
    Vector3 GrowBranch(Vector3 start, Vector3 dir, float length, float width, int gen)
    {
        dir.Normalize();
        Vector3 perp = Vector3.Cross(dir, Random.onUnitSphere).normalized;
        Vector3 mid = start + dir * (length * 0.5f) + perp * (length * curl); // making sure my branches are curled
        Vector3 end = start + dir * length;

        // using a quadratic Bezier for a smooth curve, no corners
        GameObject branchGO = new GameObject($"Branch_g{gen}");
        branchGO.transform.SetParent(transform, true);
        branchGO.layer = gameObject.layer;

        LineRenderer lr = branchGO.AddComponent<LineRenderer>();
        lr.useWorldSpace = true;
        lr.material = branchMaterial;
        lr.numCapVertices = 4;
        lr.numCornerVertices = 4;
        lr.alignment = LineAlignment.View;
        lr.textureMode = LineTextureMode.Stretch;
        lr.positionCount = branchResolution;

        AnimationCurve taper = new AnimationCurve( // Tapering the width along the branch whilt it grows, so it looks realistic
            new Keyframe(0f, width),
            new Keyframe(1f, width * 0.3f));
        lr.widthCurve = taper;

        for (int i = 0; i < branchResolution; i++)
        { float t = i / (float)(branchResolution - 1); 
        Vector3 p = QuadraticBezier(start, mid, end, t); 
        lr.SetPosition(i, p);}

        StartCoroutine(AnimateBranchGrow(lr, start, mid, end));
        return end;
    }

    IEnumerator AnimateBranchGrow(LineRenderer lr, Vector3 a, Vector3 b, Vector3 c)
    {
        float dur = 0.35f; float elapsed = 0f; int res = lr.positionCount;

        while (elapsed < dur && lr != null)
        {
            elapsed += Time.deltaTime;
            float grow = Mathf.Clamp01(elapsed / dur);

            for (int i = 0; i < res; i++) {
                float t = i / (float)(res - 1) * grow;
                lr.SetPosition(i, QuadraticBezier(a, b, c, t));
            }
            yield return null;
        }
    }

    void SpawnBlob(Vector3 pos, int gen, float scaleOverride = -1f)
    {
        if (blobPrefab == null) return;

        GameObject blob = Instantiate(blobPrefab, pos, Quaternion.identity, transform);
        blob.layer = gameObject.layer;

        float target = scaleOverride > 0f ? scaleOverride : baseBlobScale * Mathf.Pow(blobFalloff, gen);
        StartCoroutine(AnimateBlobGrow(blob.transform, target));
    }

    IEnumerator AnimateBlobGrow(Transform blob, float targetScale) 
    {
        float elapsed = 0f;
        Vector3 final = Vector3.one * targetScale;

        while (elapsed < blobGrowTime && blob != null)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / blobGrowTime;
            float eased = 1f - Mathf.Pow(1f - t, 3f); // easing-out for smoothness
            blob.localScale = final * eased;
            yield return null;
        }
        if (blob != null) blob.localScale = final;
    }

// ----------------------------
// maths funcs im using!
// pasting the quadratic bezier, cone direction maths functions i used:
// referenced https://stackoverflow.com/questions/30339226/finding-points-on-a-quadratic-bezier-curve-path
//

    static Vector3 QuadraticBezier(Vector3 a, Vector3 b, Vector3 c, float t)
    {
        float u = 1f - t;
        return u * u * a + 2f * u * t * b + t * t * c;
    }

    static Vector3 RandomConeDirection(Vector3 axis, float angleDegrees)
    {
        axis.Normalize();
        float angle = Random.Range(0f, angleDegrees) * Mathf.Deg2Rad;
        float spin = Random.Range(0f, 360f) * Mathf.Deg2Rad;

        Vector3 perp = Vector3.Cross(axis, Random.onUnitSphere).normalized;
        Vector3 spun = Quaternion.AngleAxis(spin * Mathf.Rad2Deg, axis) * perp;

        return (Mathf.Cos(angle) * axis + Mathf.Sin(angle) * spun).normalized;
    }
}