using UnityEngine;

public class SceneSwitch : MonoBehaviour
{
    public void SwitchScene(string name)
    {
        SceneTransition.GetInstance().TransitionToScene(name);
    }
}
