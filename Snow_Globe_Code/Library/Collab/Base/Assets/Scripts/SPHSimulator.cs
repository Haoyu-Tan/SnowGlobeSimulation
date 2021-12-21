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

    //water simulation
    public Vector3 gravity = new Vector3(0,-9.8f,0);
    //public Vector3 liftForce = new Vector3(0, 50f, 0);
    private int numParticles;
    private int maxParticles = 1000;
    public GameObject waterPrefab;
    WaterParticles[] wp;
    float[,] distPairs;
    Vector3[,] dirPairs;

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
    private float snow_inter_rad_h = 0.5f;
    private float snow_v_k = 2f;
    private float bounce_keep = 0.8f;

    private float ss_k_stiff = 1f;
    private float ss_k_near = 0.1f;
    private float ss_p_0 = 0.01f;
    private float ss_inter_rad_h = 0.3f;

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
    private float damping = 1f;

    void Start()
    {
        numParticles = 0;
        wp = new WaterParticles[maxParticles];
        // Generate Particles
        //int index = 0;
        // -1 to 1
        Vector3 baseVec = new Vector3(-1.5f, -1.5f, -1.5f);

        for(int i = 0; i < 9; i += 1)
        {
            for (int j = 0; j < 9; j += 1)
            {
                for(int k = 0; k < 9; k += 1)
                {
                    Vector3 pos = new Vector3(i * 0.375f, j * 0.375f, k * 0.375f);
                    pos += baseVec;
                    if(pos.magnitude + 0.05f > globeRad)
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

        distPairs = new float[numParticles,numParticles];
        dirPairs = new Vector3[numParticles, numParticles];
        for(int i = 0; i < numParticles; i++)
        {
            distPairs[i, i] = -1;
            dirPairs[i, i] = Vector3.zero;
        }

        Debug.Log("We have " + numParticles + "particles");

        initSnow();
    }

    void initSnow() {
        numSnow = 0;
        sp = new SnowParticle[maxNumSnow];

        Vector3 initPos = new Vector3(-1.5f, -1.5f, -1.5f);

        for (int i = 0; i < 10; i++) {
            for (int j = 0; j < 10; j++) {
                for (int k = 0; k < 10; k++) {
                    Vector3 pos = new Vector3(i * 0.34f, j * 0.34f, k * 0.34f);
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
        viscosity();
        // first position update
        firstPosUpdate();
        // update pairs
        distPairsUpdate();
        // double density relaxation
        doubleDensityRelaxation();
        // Resolve Collision
        resolveCollision();
        // Recalculate Vel
        recalVelocity();


    }

    void SnowUpdate() {
        for (int i = 0; i < numSnow; i++)
        {
            sp[i].UpdateCube();
            sp[i].UpdateNeighborCube(snow_inter_rad_h); //
        }

        sdistPairsUpdate();

        SnowUpdateVelocity();

        SnowUpdatePosition();

        //ddr
        for (int i = 0; i < numSnow; i++)
        {
            sp[i].UpdateCube();
            sp[i].UpdateNeighborCube(ss_inter_rad_h); //
        }

        SnowSnowPair();

        SnowDoubleDensityRelaxation();

        ResolveSnowCollision();

        //SnowRecalVelocity();
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
            if (counter > 0) deltaV /= counter;

            sp[i].velocity += deltaV;
        }
    }

    void SnowUpdatePosition() {
        for (int i = 0; i < numSnow; i++)
        {
            // save previous position
            sp[i].prevPos = sp[i].transform.localPosition;
            sp[i].prevPosGlobal = sp[i].transform.position;
            sp[i].transform.localPosition += (sp[i].velocity * Time.deltaTime);
        }
    }



    void updateSnowList()
    {
        // each snow assigned to a list
        // 
    }


    int  IntToVect(Vector3 input, int dim)
    {
        // Hash input Vector 3 to its key
        int result = -1;
        return result;
    }

    Vector3 VecToInt(int input, int dim)
    {
        // Hash input back to Vector 3
        Vector3 result = Vector3.zero;
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



        for (int i = 0; i < numSnow - 1; i++)
        {
            for (int j = i + 1; j < numSnow; j++)
            {
                //calculate distance
                
                if (!sp[i].myNeighborCube.Contains(sp[j].currentCube)) {
                    ssdistPairs[i, j] = -1;
                    ssdistPairs[j, i] = -1;
                    continue;
                }

                Vector3 dir = sp[j].transform.localPosition - sp[i].transform.localPosition;
                float dist = dir.magnitude;
                float val = dist / ss_inter_rad_h;
                val = 1 - val;

                ssdistPairs[i, j] = val;
                ssdistPairs[j, i] = val;

                if (val < 0) continue;

                dir = dir / dist;
                ssdirPairs[i, j] = dir;
                ssdirPairs[j, i] = (-1f) * dir;
                
            }
        }
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

            if (distToGlob - (globeRad - snowRad) > threshold)
            {
                //collide    
                Vector3 norm = (sp[i].transform.localPosition).normalized; // dir center to local position
                sp[i].transform.localPosition = center + norm * (globeRad - snowRad - threshold);
                //Vector3 collisionVel = -Vector3.Dot(sp[i].velocity, norm)*norm;
                //sp[i].velocity = sp[i].velocity + (1 + bounce_keep) * collisionVel;
                //sp[i].velocity = (sp[i].transform.localPosition - sp[i].prevPos) / Time.deltaTime;
                //Vector3 proj = Vector3.Dot(wp[i].velocity, norm) * norm;

                //wp[i].velocity -= (1 + cor) * proj;
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

                    //sp[i].velocity = (sp[i].transform.localPosition - sp[i].prevPos) / Time.deltaTime;
                }
            }
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
        Vector3 toSpeed = lifter.GetComponent<Lifter>().myFinalVelocity;
        float deltaV = lifter.GetComponent<Lifter>().acc * Time.deltaTime;
        for (int i = 0; i < numParticles; i++)
        {
            wp[i].velocity += (gravity * Time.deltaTime);
            
            if (lifter.bounds.Contains(wp[i].transform.position))
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


    void viscosity()
    {

    }


    void firstPosUpdate()
    {
        for (int i = 0; i < numParticles; i++)
        {
            // save previous position
            wp[i].previousPos = wp[i].transform.localPosition;
            wp[i].transform.localPosition += (wp[i].velocity * Time.deltaTime);

            wp[i].UpdateCube();
            wp[i].UpdateNeighborCube(inter_rad_h); //
        }
    }

    void sdistPairsUpdate() {
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
            wp[i].velocity *= damping;
        }

    }
}
