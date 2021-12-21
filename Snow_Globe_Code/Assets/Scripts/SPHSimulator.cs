using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SPHSimulator : MonoBehaviour
{
    //Serialize
    [SerializeField]
    private Transform snowGlob;
    [SerializeField]
    private BoxCollider lifter;
    [SerializeField]
    private BoxCollider doRayCast;
    [SerializeField]
    private Grabbable myParent;

    //water simulation
    public Vector3 gravity = new Vector3(0, -9.8f, 0);
    //public Vector3 liftForce = new Vector3(0, 50f, 0);
    private int numParticles;
    private int maxParticles = 1000;
    public GameObject waterPrefab;
    WaterParticles[] wp;
    float[,] distPairs;
    Vector3[,] dirPairs;
    // water spatial
    int w_dim;
    List<int>[] w_hash_grid;
    private float k_stir = 2;

    //snow particle simulation
    private int numSnow;
    private int maxNumSnow = 1000;
    public GameObject snowPrefab;
    SnowParticle[] sp;
    float[,] sdistPairs;
    //Vector3[,] sdirPairs;
    float[,] ssdistPairs;
    Vector3[,] ssdirPairs;

    //snow constant
    private float snow_inter_rad_h = 0.3f;
    private float snow_v_k = 1f;
    private float bounce_keep = 0.8f;

    private float ss_k_stiff = 0.5f;
    private float ss_k_near = 0.01f;
    private float ss_p_0 = 1f;
    private float ss_inter_rad_h = 0.3f;

    //Snow snow speed up
    int ss_dim;
    List<int>[] ss_hash_grid;

    // Snow Water speed up
    int sw_dim;
    List<int>[] snow_hash_grid;
    List<int>[] water_hash_grid;

    //simulation constant

    //viscosity
    private float vis_alpha;
    private float vis_beta;

    //double density relaxation

    private float inter_rad_h = 0.85f;
    private float k_stiff = 10f;
    private float k_near = 1f;
    private float p_0 = 1f; //rest length density for particle

    //resolve collision
    private float cor = 0.8f;
    private float globeRad = 1.5f; // radius of globe
    private Vector3 center = Vector3.zero;

    // recalculate velocity
    float minY = -0.8f;
    private float damping = 0.8f;

    void Start()
    {
        numParticles = 0;
        wp = new WaterParticles[maxParticles];
        // Generate Particles
        //int index = 0;
        // -1 to 1
        Vector3 baseVec = new Vector3(-1.5f, -1.5f, -1.5f);

        for (int i = 0; i < 9; i += 1)
        {
            for (int j = 0; j < 9; j += 1)
            {
                for (int k = 0; k < 9; k += 1)
                {
                    Vector3 pos = new Vector3(i * 0.375f, j * 0.375f, k * 0.375f);
                    pos += baseVec;
                    if (pos.magnitude + 0.05f > globeRad)
                    {
                        continue;
                    }
                    pos += this.transform.position;
                    Quaternion rot = Quaternion.Euler(new Vector3(0, 0, 0));
                    GameObject go = Instantiate(waterPrefab, pos, rot, this.transform);
                    wp[numParticles] = go.GetComponent<WaterParticles>();
                    numParticles += 1;

                }
            }
        }

        distPairs = new float[numParticles, numParticles];
        dirPairs = new Vector3[numParticles, numParticles];
        for (int i = 0; i < numParticles; i++)
        {
            distPairs[i, i] = -1;
            dirPairs[i, i] = Vector3.zero;
        }

        Debug.Log("We have " + numParticles + "particles");

        // initialize spatial list water
        w_dim = (int)Mathf.Ceil(globeRad * 2 /inter_rad_h);
        Debug.Log(w_dim);
        w_hash_grid = new List<int>[(int)Mathf.Pow(w_dim, 3)];



        initSnow();
    }

    void initSnow() {
        numSnow = 0;
        sp = new SnowParticle[maxNumSnow];

        Vector3 initPos = new Vector3(-1f, -1f, -1f);

        for (int i = 0; i < 8; i++) {
            for (int j = 0; j < 8; j++) {
                for (int k = 0; k < 8; k++) {
                    Vector3 pos = new Vector3(i * 0.25f, j * 0.25f, k * 0.25f);
                    pos += initPos;
                    if (pos.magnitude + 0.05f > globeRad)
                    {
                        continue;
                    }
                    pos += this.transform.position;
                    Quaternion rot = Quaternion.Euler(Vector3.zero);
                    GameObject go = Instantiate(snowPrefab, pos, rot, this.transform);
                    sp[numSnow] = go.GetComponent<SnowParticle>();

                    numSnow++;
                }
            }
        }

        sdistPairs = new float[numSnow, numParticles];
        //sdirPairs = new Vector3[numSnow, numParticles];
        for (int i = 0; i < numSnow; i++)
        {
            for (int j = 0; j < numParticles; j++) {
                sdistPairs[i, j] = -1;
                //sdirPairs[i, j] = Vector3.zero;
            }
        }

        ssdistPairs = new float[numSnow, numSnow];
        ssdirPairs = new Vector3[numSnow, numSnow];

        for (int i = 0; i < numSnow; i++) {
            for (int j = 0; j < numSnow; j++) {
                ssdistPairs[i, j] = -1;
                ssdirPairs[i, j] = Vector3.zero;
            }
        }
        Debug.Log("We have " + numSnow + "snows");

        ss_dim = (int)Mathf.Ceil(globeRad * 2 / ss_inter_rad_h);
        ss_hash_grid = new List<int>[(int)Mathf.Pow(ss_dim, 3)];


        sw_dim = (int)Mathf.Ceil(globeRad * 2 / snow_inter_rad_h);
        snow_hash_grid = new List<int>[(int)Mathf.Pow(sw_dim, 3)];
        water_hash_grid = new List<int>[(int)Mathf.Pow(sw_dim, 3)];
    }

    // Update is called once per frame
    void Update()
    {
        Debug.Log("Frame rate is " + 1.0f / Time.deltaTime);
        SPHUpdate();
        SnowUpdate();
    }


    void SPHUpdate()
    {
        // apply force
        applyForce();
        // apply viscosity
        //viscosity();
        // first position update
        firstPosUpdate();
        // update pairs
        //distPairsUpdate();
        UpdateWaterList();
        WaterWaterPair();
        // double density relaxation
        doubleDensityRelaxation();
        // Resolve Collision
        resolveCollision();
        // Recalculate Vel
        recalVelocity();


    }

    void SnowUpdate() {
        /*
        for (int i = 0; i < numSnow; i++)
        {
            sp[i].UpdateCube();
            sp[i].UpdateNeighborCube(snow_inter_rad_h); //
        }
        */

        
        UpdateSnowWaterList();
        SnowWaterPair();


        //sdistPairsUpdate();

        SnowUpdateVelocity();

        SnowUpdatePosition();

        //ddr
        /**
        for (int i = 0; i < numSnow; i++)
        {
            sp[i].UpdateCube();
            sp[i].UpdateNeighborCube(ss_inter_rad_h); //
        }
        */

        UpdateSnowList();

        SnowSnowPair();

        SnowDoubleDensityRelaxation();

        ResolveSnowCollision();

        SnowRecalVelocity();
    }

    void SnowUpdateVelocity() {
        for (int i = 0; i < numSnow; i++) {
            //apply gravity
            sp[i].velocity += gravity * Time.deltaTime;

            Vector3 deltaV = Vector3.zero;
            int counter = 0;
            for (int j = 0; j < numParticles; j++) {
                float val = sdistPairs[i, j];
                if (val > 0) {
                    deltaV += val * (wp[j].velocity - sp[i].velocity);
                    counter++;
                }
            }
            deltaV *= snow_v_k;
            //if (counter > 0) deltaV /= counter;

            sp[i].velocity += deltaV;
        }
    }

    void SnowUpdatePosition() {
        for (int i = 0; i < numSnow; i++)
        {
            // save previous position
            sp[i].prevPos = sp[i].transform.localPosition;
            sp[i].prevPosGlobal = sp[i].transform.position;
            sp[i].transform.position += (sp[i].velocity * Time.deltaTime);
        }
    }





    void UpdateSnowList()
    {
        for(int i = 0; i < ss_hash_grid.Length; i++)
        {
            ss_hash_grid[i] = new List<int>();
        }
        // each snow assigned to a list
        for (int i = 0; i < numSnow; i++) {
            Vector3 temp = sp[i].transform.localPosition + new Vector3(globeRad, globeRad, globeRad);
            //Debug.Log("Original is " + temp);
            temp = temp / ss_inter_rad_h;
            temp = new Vector3(Mathf.Floor(temp.x), Mathf.Floor(temp.y), Mathf.Floor(temp.z));
            int maxVal = ss_dim - 1;
            temp.x = Mathf.Clamp(temp.x, 0, maxVal);
            temp.y = Mathf.Clamp(temp.y, 0, maxVal);
            temp.z = Mathf.Clamp(temp.z, 0, maxVal);
            //Debug.Log("After is " + temp);
            
            int result = Vec2Int(temp, ss_dim);
            //Debug.Log(result);
            ss_hash_grid[result].Add(i);
        }
    }


    int Vec2Int(Vector3 input, int dim)
    {
        // Hash input Vector 3 to its key
        // hash = i * dim * dim + j * dim + k
        //int result = ;

        return (int)(input.x * Mathf.Pow(dim, 2) + input.y * dim + input.z);
    }

    Vector3 Int2Vec(int input, int dim)
    {
        // Hash input back to Vector 3
        Vector3 result;
        result.z = input % dim;
        input /= dim;
        result.y = input % dim;
        result.x = input / dim;

        return result;
    }

    void SnowSnowPair() {
        //--------------------------
        // FIX HERE, After you build the data structure
        //---------------------------

        // for Q in size of list we have
        // i -1 -> 1  j -1 -> 1  k -1 -> 1
        // update P using Vec3 <i,j,k>
        // if P >= Q we will update the distance pair
        // sp left = sp[List(P)] sp_Right = sp[List(q)]

        for (int i = 0; i < numSnow; i++) {
            for (int j = 0; j < numSnow; j++) {
                ssdistPairs[i, j] = -1f;
            }
        }

        for (int Q = 0; Q < ss_hash_grid.Length; Q++) {
            Vector3 index = Int2Vec(Q, ss_dim);

            for (int i = -1; i <= 1; i++) {
                int currI = (int)index.x + i;
                if (currI < 0) continue;
                if (currI >= ss_dim) continue;

                for (int j = -1; j <= 1; j++) {
                    int currJ = (int)index.y + j;
                    if (currJ < 0) continue;
                    if (currJ >= ss_dim) continue;

                    for (int k = -1; k <= 1; k++) {
                        int currK = (int)index.z + k;
                        if (currK < 0) continue;
                        if (currK >= ss_dim) continue;

                        int P = Vec2Int(new Vector3(currI, currJ, currK), ss_dim);

                        if (P == Q) {
                            //int the area Q
                            for (int l = 0; l < ss_hash_grid[P].Count - 1; l++)
                            {
                                for (int m = l + 1; m < ss_hash_grid[P].Count; m++)
                                {
                                    SSCalculateDistPair(ss_hash_grid[P][l], ss_hash_grid[P][m]);
                                }
                            }
                        }
                        else if (P > Q) {
                            for (int l = 0; l < ss_hash_grid[Q].Count; l++)
                            {
                                for (int m = 0; m < ss_hash_grid[P].Count; m++)
                                {
                                    SSCalculateDistPair(ss_hash_grid[Q][l], ss_hash_grid[P][m]);
                                }
                            }
                        }
                    }
                }
            }
        }
    }

    void SSCalculateDistPair(int i, int j) {

        Vector3 dir = sp[j].transform.localPosition - sp[i].transform.localPosition;
        float dist = dir.magnitude;
        float val = dist / ss_inter_rad_h;
        val = 1 - val;

        ssdistPairs[i, j] = val;
        ssdistPairs[j, i] = val;

        if (val < 0) return;

        dir = dir / dist;
        ssdirPairs[i, j] = dir;
        ssdirPairs[j, i] = (-1f) * dir;

    }

    void SnowDoubleDensityRelaxation() {
        for (int i = 0; i < numSnow; i++) {
            float p = 0f;
            float p_near = 0f;

            for (int j = 0; j < numSnow; j++) {
                float val = ssdistPairs[i, j];
                if (val > 0) {
                    p += Mathf.Pow(val, 2);
                    p_near += Mathf.Pow(val, 3);
                }
            }

            //!!!!!!!!!!!!
            float P = ss_k_stiff * (p - ss_p_0);
            float P_near = ss_k_near * p_near;



            Vector3 dx = Vector3.zero;

            for (int j = 0; j < numSnow; j++){
                float val = ssdistPairs[i, j];

                if (val > 0) {
                    Vector3 dir = ssdirPairs[i, j];
                    Vector3 D = Mathf.Pow(Time.deltaTime, 2) * (P * val + P_near * Mathf.Pow(val, 2)) * dir;
                    sp[j].transform.localPosition += D / 2;
                    dx -= D / 2;
                }

            }

            sp[i].transform.localPosition += dx;

        }
    }

    void ResolveSnowCollision() {
        for (int i = 0; i < numSnow; i++)
        {
            float threshold = 0.001f;
            if (sp[i].transform.localPosition.y < minY + threshold)
            {
                Vector3 pos = sp[i].transform.localPosition;
                pos.y = minY + threshold;
                sp[i].transform.localPosition = pos;
            }
            //(wp[i].position - glob.position).mag
            float distToGlob = sp[i].transform.localPosition.magnitude;

            //float snowRad = snowGlob.localScale.x / 2;
            float snowRad = sp[i].transform.localScale.x / 2;
            //sp[i].velocity = (sp[i].transform.localPosition - sp[i].prevPos) / Time.deltaTime;

            if (distToGlob - (globeRad - snowRad) > threshold)
            {
                //collide    
                Vector3 norm = (sp[i].transform.localPosition).normalized; // dir center to local position
                sp[i].transform.localPosition = center + norm * (globeRad - snowRad - threshold);
                //Vector3 collisionVel = -Vector3.Dot(sp[i].velocity, norm)*norm;
                //sp[i].velocity = sp[i].velocity + (1 + bounce_keep) * collisionVel;
                //sp[i].velocity = (sp[i].transform.localPosition - sp[i].prevPos) / Time.deltaTime;
                //Vector3 proj = Vector3.Dot(wp[i].velocity, norm) * norm;

                //sp[i].velocity -= (1 + cor) * proj;
            }

        }

        for (int i = 0; i < numSnow; i++)
        {
            if (doRayCast.bounds.Contains(sp[i].transform.position))
            {
                RaycastHit hit;
                Vector3 dir = sp[i].transform.position - sp[i].prevPosGlobal;
                float dist = dir.magnitude;
                int layerMask = 1 << 8;
                dir.Normalize();
                // Does the ray intersect any objects excluding the player layer
                if (Physics.Raycast(sp[i].prevPosGlobal, dir, out hit, dist, layerMask))
                {
                    if (Vector3.Dot(hit.normal, dir) > 0)
                    {
                        sp[i].transform.position = hit.point - 0.001f * hit.normal;
                    }

                    else
                    {
                        sp[i].transform.position = hit.point + 0.001f * hit.normal;
                    }

                    
                }
            }
            //sp[i].velocity = (sp[i].transform.localPosition - sp[i].prevPos) / Time.deltaTime;
        }
    }

    void SnowRecalVelocity()
    {
        for (int i = 0; i < numSnow; i++)
        {
            sp[i].velocity = (sp[i].transform.localPosition - sp[i].prevPos) / Time.deltaTime;
            // apply damping
            sp[i].velocity *= damping;
        }

    }

    void applyForce()
    {
        Vector3 toSpeed = Vector3.zero;
        float deltaV = 0.0f;
        if (lifter.enabled)
        {
            toSpeed = lifter.GetComponent<Lifter>().myFinalVelocity;
            deltaV = lifter.GetComponent<Lifter>().acc * Time.deltaTime;
        }


        for (int i = 0; i < numParticles; i++)
        {
            wp[i].velocity += (gravity * Time.deltaTime);
            wp[i].velocity -= myParent.deltaV * k_stir;
            
            if (lifter.enabled && lifter.bounds.Contains(wp[i].transform.position))
            {
                Vector3 deltaSpeed = toSpeed - wp[i].velocity;
                float mag = deltaSpeed.magnitude;

                if (mag > deltaV)
                {
                    deltaSpeed = deltaSpeed / mag;
                    wp[i].velocity += deltaV * deltaSpeed;
                }
                else
                {
                    wp[i].velocity = toSpeed;
                }
            }
            
        }
    }

    void firstPosUpdate()
    {
        for (int i = 0; i < numParticles; i++)
        {
            // save previous position
            wp[i].previousPos = wp[i].transform.localPosition;
            wp[i].transform.position += (wp[i].velocity * Time.deltaTime);

            //wp[i].UpdateCube();
            //wp[i].UpdateNeighborCube(inter_rad_h); //
        }
    }

    void UpdateWaterList()
    {
        for (int i = 0; i < w_hash_grid.Length; i++)
        {
            w_hash_grid[i] = new List<int>();
        }
        // each snow assigned to a list
        for (int i = 0; i < numParticles; i++)
        {
            Vector3 temp = wp[i].transform.localPosition + new Vector3(globeRad, globeRad, globeRad);
            //Debug.Log("Original is " + temp);
            temp = temp / inter_rad_h;
            temp = new Vector3(Mathf.Floor(temp.x), Mathf.Floor(temp.y), Mathf.Floor(temp.z));
            int maxVal = w_dim - 1;
            temp.x = Mathf.Clamp(temp.x, 0, maxVal);
            temp.y = Mathf.Clamp(temp.y, 0, maxVal);
            temp.z = Mathf.Clamp(temp.z, 0, maxVal);
            //Debug.Log("After is " + temp);

            int result = Vec2Int(temp, w_dim);
            //Debug.Log(result);
            w_hash_grid[result].Add(i);
        }
    }

    // Calculate the Dist Map for Water
    void WaterWaterPair()
    {
        //--------------------------
        // FIX HERE, After you build the data structure
        //---------------------------

        // for Q in size of list we have
        // i -1 -> 1  j -1 -> 1  k -1 -> 1
        // update P using Vec3 <i,j,k>
        // if P >= Q we will update the distance pair
        // sp left = sp[List(P)] sp_Right = sp[List(q)]

        for (int i = 0; i < numParticles; i++)
        {
            for (int j = 0; j < numParticles; j++)
            {
                distPairs[i, j] = -1f;
            }
        }

        for (int Q = 0; Q < w_hash_grid.Length; Q++)
        {
            Vector3 index = Int2Vec(Q, w_dim);

            for (int i = -1; i <= 1; i++)
            {
                int currI = (int)index.x + i;
                if (currI < 0) continue;
                if (currI >= w_dim) continue;

                for (int j = -1; j <= 1; j++)
                {
                    int currJ = (int)index.y + j;
                    if (currJ < 0) continue;
                    if (currJ >= w_dim) continue;

                    for (int k = -1; k <= 1; k++)
                    {
                        int currK = (int)index.z + k;
                        if (currK < 0) continue;
                        if (currK >= w_dim) continue;

                        int P = Vec2Int(new Vector3(currI, currJ, currK), w_dim);

                        if (P == Q)
                        {
                            //int the area Q
                            for (int l = 0; l < w_hash_grid[P].Count - 1; l++)
                            {
                                for (int m = l + 1; m < w_hash_grid[P].Count; m++)
                                {
                                    WaterCalculateDistPair(w_hash_grid[P][l], w_hash_grid[P][m]);
                                }
                            }
                        }
                        else if (P > Q)
                        {
                            for (int l = 0; l < w_hash_grid[Q].Count; l++)
                            {
                                for (int m = 0; m < w_hash_grid[P].Count; m++)
                                {
                                    WaterCalculateDistPair(w_hash_grid[Q][l], w_hash_grid[P][m]);
                                }
                            }
                        }
                    }
                }
            }
        }
    }

    // Snow and Water Influence
    void UpdateSnowWaterList()
    {
        for (int i = 0; i < snow_hash_grid.Length; i++)
        {
            snow_hash_grid[i] = new List<int>();
            water_hash_grid[i] = new List<int>();
        }
        // each snow assigned to a list
        for (int i = 0; i < numSnow; i++)
        {
            Vector3 temp = sp[i].transform.localPosition + new Vector3(globeRad, globeRad, globeRad);
            //Debug.Log("Original is " + temp);
            temp = temp / snow_inter_rad_h;
            temp = new Vector3(Mathf.Floor(temp.x), Mathf.Floor(temp.y), Mathf.Floor(temp.z));
            int maxVal = sw_dim - 1;
            temp.x = Mathf.Clamp(temp.x, 0, maxVal);
            temp.y = Mathf.Clamp(temp.y, 0, maxVal);
            temp.z = Mathf.Clamp(temp.z, 0, maxVal);
            //Debug.Log("After is " + temp);

            int result = Vec2Int(temp, sw_dim);
            //Debug.Log(result);
            snow_hash_grid[result].Add(i);
        }

        for (int i = 0; i < numParticles; i++)
        {
            Vector3 temp = wp[i].transform.localPosition + new Vector3(globeRad, globeRad, globeRad);
            //Debug.Log("Original is " + temp);
            temp = temp / snow_inter_rad_h;
            temp = new Vector3(Mathf.Floor(temp.x), Mathf.Floor(temp.y), Mathf.Floor(temp.z));
            int maxVal = sw_dim - 1;
            temp.x = Mathf.Clamp(temp.x, 0, maxVal);
            temp.y = Mathf.Clamp(temp.y, 0, maxVal);
            temp.z = Mathf.Clamp(temp.z, 0, maxVal);
            //Debug.Log("After is " + temp);

            int result = Vec2Int(temp, sw_dim);
            //Debug.Log(result);
            water_hash_grid[result].Add(i);
        }
    }


    void SnowWaterPair()
    {
        //--------------------------
        // FIX HERE, After you build the data structure
        //---------------------------

        // for Q in size of list we have
        // i -1 -> 1  j -1 -> 1  k -1 -> 1
        // update P using Vec3 <i,j,k>
        // if P >= Q we will update the distance pair
        // sp left = sp[List(P)] sp_Right = sp[List(q)]

        for (int i = 0; i < numSnow; i++)
        {
            for (int j = 0; j < numParticles; j++)
            {
                sdistPairs[i, j] = -1f;
            }
        }

        for (int Q = 0; Q < snow_hash_grid.Length; Q++)
        {
            Vector3 index = Int2Vec(Q, sw_dim);

            for (int i = -1; i <= 1; i++)
            {
                int currI = (int)index.x + i;
                if (currI < 0) continue;
                if (currI >= sw_dim) continue;

                for (int j = -1; j <= 1; j++)
                {
                    int currJ = (int)index.y + j;
                    if (currJ < 0) continue;
                    if (currJ >= sw_dim) continue;

                    for (int k = -1; k <= 1; k++)
                    {
                        int currK = (int)index.z + k;
                        if (currK < 0) continue;
                        if (currK >= sw_dim) continue;

                        int P = Vec2Int(new Vector3(currI, currJ, currK), sw_dim);

                        
                        for (int l = 0; l < snow_hash_grid[Q].Count; l++)
                        {
                            for (int m = 0; m < water_hash_grid[P].Count; m++)
                            {
                                SnowWaterCalculateDistPair(snow_hash_grid[Q][l], water_hash_grid[P][m]);
                            }
                        }
                    }
                }
            }
        }
    }


    /*
    void sdistPairsUpdate() {
        // Next to Fix


        for (int i = 0; i < numSnow; i++) {
            for (int j = 0; j < numParticles; j++) {
                if (!sp[i].myNeighborCube.Contains(wp[j].currentCube)) {
                    sdistPairs[i, j] = -1;
                    //sdistPa0000000000irs[j, i] = -1;
                    continue;
                }

                Vector3 dir = wp[j].transform.localPosition - sp[i].transform.localPosition;
                float dist = dir.magnitude;
                dist = dist / snow_inter_rad_h;
                dist = 1 - dist;

                sdistPairs[i, j] = dist;
                //sdistPairs[j, i] = dist;
                //if (dist < 0) continue;

                //dir = dir.normalized;
                //sdirPairs[i, j] = dir;
                //sdirPairs[j, i] = -1 * dir;
            }
        }
    }
    */

    void SnowWaterCalculateDistPair(int i, int j)
    {

        Vector3 dir = wp[j].transform.localPosition - sp[i].transform.localPosition;
        float dist = dir.magnitude;
        dist = dist / snow_inter_rad_h;
        dist = 1 - dist;
        sdistPairs[i, j] = dist;
    }


    void WaterCalculateDistPair(int i, int j)
    {

        Vector3 dir = wp[j].transform.localPosition - wp[i].transform.localPosition;
        float dist = dir.magnitude;
        float val = dist / inter_rad_h;
        val = 1 - val;

        distPairs[i, j] = val;
        distPairs[j, i] = val;

        if (val < 0) return;

        dir = dir / dist;
        dirPairs[i, j] = dir;
        dirPairs[j, i] = (-1f) * dir;

    }
    /*
    void distPairsUpdate()
    {
        for (int i = 0; i < numParticles - 1; i++)
        {
            for(int j = i+1; j < numParticles; j++)
            {
                // calculate distance
                if (!wp[i].myNeighborCube.Contains(wp[j].currentCube))
                {
                    distPairs[i, j] = -1;
                    distPairs[j, i] = -1;
                    continue;
                }
                Vector3 dir = wp[j].transform.localPosition - wp[i].transform.localPosition;
                float dist = dir.magnitude;
                float val = dist/ inter_rad_h;
                val = 1 - val;
                distPairs[i, j] = val;
                distPairs[j, i] = val;
                if(val < 0)
                {
                    continue;
                }
                dir = dir / dist;
                dirPairs[i, j] = dir;
                dirPairs[j, i] = -1 * dir;
                // actually it is rij/h
            }
        }
    }
    */



    // p => density, P => Pressure
    void doubleDensityRelaxation()
    {
        for (int i = 0; i < numParticles; i++) {
            float p = 0;
            float p_near = 0;
            //compute density and near-density
            for (int j = 0; j < numParticles; j++) {
                float val = distPairs[i,j]; // value calculated from dist
                if(val > 0)
                {
                    p += Mathf.Pow(val, 2);
                    p_near += Mathf.Pow((val), 3);
                }
            }

            //compute pressure and near pressure
            float P = k_stiff * (p - p_0); // P <- k(p - p0)
            float P_near = k_near * p_near;

            Vector3 dx = Vector3.zero;

            for (int j = 0; j < numParticles; j++) {
                //q <-- r_ij / h
                float val = distPairs[i, j]; // value calculated from dist

                if (val > 0)
                {
                    //apply displacements

                    Vector3 dir = dirPairs[i, j];
                    Vector3 D = Mathf.Pow(Time.deltaTime, 2) * (P * val + P_near * Mathf.Pow(val, 2)) * dir;

                    wp[j].transform.localPosition += D / 2;
                    dx -= D / 2;
                }
            }

            wp[i].transform.localPosition += dx;

        }
    }

    /*
    void doubleDensityRelaxationV0()
    {
        for (int i = 0; i < numParticles; i++)
        {
            float p = 0;
            float p_near = 0;
            // compute the current neighbor cubes
            wp[i].UpdateNeighborCube(inter_rad_h);
            //compute density and near-density
            for (int j = 0; j < numParticles; j++)
            {
                if (!wp[i].myNeighborCube.Contains(wp[j].currentCube))
                    continue;
                //q <-- r_ij / h
                float q = Vector3.Distance(wp[i].transform.localPosition, wp[j].transform.localPosition) / inter_rad_h;

                if (i != j && q < 1)
                {
                    p += Mathf.Pow((1 - q), 2); // p <-- (1 - q)^2
                    p_near += Mathf.Pow((1 - q), 3); // p <-- (1-q)^3
                }
            }

            //compute pressure and near pressure
            float P = k_stiff * (p - p_0); // P <- k(p - p0)
            float P_near = k_near * p_near;

            Vector3 dx = Vector3.zero;

            for (int j = 0; j < numParticles; j++)
            {
                if (!wp[i].myNeighborCube.Contains(wp[j].currentCube))
                    continue;
                //q <-- r_ij / h
                float q = Vector3.Distance(wp[i].transform.localPosition, wp[j].transform.localPosition) / inter_rad_h;

                if (i != j && q < 1)
                {
                    //apply displacements
                    Vector3 rDir_ij = (wp[j].transform.localPosition - wp[i].transform.localPosition).normalized;
                    Vector3 D = Mathf.Pow(Time.deltaTime, 2) * (P * (1 - q) + P_near * Mathf.Pow((1 - q), 2)) * rDir_ij;

                    wp[j].transform.localPosition += D / 2;
                    wp[j].UpdateCube();
                    dx -= D / 2;
                }
            }

            wp[i].transform.localPosition += dx;
            wp[i].UpdateCube();

        }
    }
    */



    void resolveCollision()
    {
        
        // collision with globe
        // collision with model in the middle
        for (int i = 0; i < numParticles; i++) {
            float threshold = 0.001f;
            if (wp[i].transform.localPosition.y < -1 + threshold)
            {
                Vector3 pos = wp[i].transform.localPosition ;
                pos.y = -1 + threshold;
                wp[i].transform.localPosition = pos;
            }
            //(wp[i].position - glob.position).mag
            float distToGlob = wp[i].transform.localPosition.magnitude;

            //float snowRad = snowGlob.localScale.x / 2;
            float waterRad = wp[i].transform.localScale.x/2;
            

            if (distToGlob - (globeRad - waterRad) > threshold) {
                //collide    
                Vector3 norm = (wp[i].transform.localPosition).normalized; // dir center to local position
                wp[i].transform.localPosition = center + norm * (globeRad - waterRad - threshold);

                
                //Vector3 proj = Vector3.Dot(wp[i].velocity, norm) * norm;

                //wp[i].velocity -= (1 + cor) * proj;
              



            }
        }
        
    }
    

    void recalVelocity()
    {
        for (int i = 0; i < numParticles; i++) {
            wp[i].velocity = (wp[i].transform.localPosition - wp[i].previousPos) / Time.deltaTime;
            // apply damping
            //wp[i].velocity *= damping;
        }

    }
}
