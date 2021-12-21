using UnityEngine;

public class Button : MonoBehaviour
{
    private float coolDown;
    [SerializeField]
    private int buttonType;
    [SerializeField]
    private GameObject[] objects;

    [SerializeField]
    private Renderer indicator;
    [SerializeField]
    private Material indicatorMat;
    [SerializeField]
    private Material originalMat;
    [SerializeField]
    private AudioSource christmasMusic;

    private int lightIndex = 1;
    
    // 0 controls the light and 1 controls the mode
    // Start is called before the first frame update
    void Start()
    {
        coolDown = 0.0f;
        indicator.material = originalMat;
        
    }


    public void ButtonDown()
    {
        if (coolDown > 0) return;
        switch (buttonType)
        {
            case 0:
                ChangeLightMode();
                break;
            case 1:
                ChangeMode();
                break;
        }

        coolDown = 1f;
    }

    public void DisableIndicator()
    {
        indicator.material = originalMat;
    }


    public void EnableIndicator()
    {
        indicator.material = indicatorMat;
    }

    private void ChangeMode()
    {
        if (objects[0].GetComponent<Lifter>())
        {
            objects[0].GetComponent<Lifter>().ChangeMode();
        }

    }

    //0: light off sound off
    //1: turn on light
    //2: turn on sound
    //3: turn off light
    public void ChangeLightMode() {
        lightIndex += 1;
        if (lightIndex > 3) lightIndex = 0;

        switch (lightIndex) {
            case 0:
                DisableLights();
                DisableMusic();
                break;
            case 1:
                EnableLights();
                break;
            case 2:
                EnableMusic();
                break;
            case 3:
                DisableLights();
                break;
        }


    }

    void EnableLights() {
        foreach (GameObject light in objects) {
            light.SetActive(true);
        }
    }

    void DisableLights() {
        foreach (GameObject light in objects) {
            light.SetActive(false);
        }
    }

    void EnableMusic() {
        christmasMusic.Play();
    }

    void DisableMusic() {
        christmasMusic.Stop();
    }

    // Update is called once per frame
    void Update()
    {
        if(coolDown > 0)
        {
            coolDown -= Time.deltaTime;
        }
    }
}
