using UnityEngine;
using UnityEngine.UI;

public class MahjongFinshTileStyle : MonoBehaviour
{
    public RawImage image;
    public GameObject final;

    public void Clear()
    {
        if (final != null)
            final.SetActive(false);

        GameObject gameObject = base.gameObject;
        if (gameObject != null)
            gameObject.SetActive(false);
    }
}
