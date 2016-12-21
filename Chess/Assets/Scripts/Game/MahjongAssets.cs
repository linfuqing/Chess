using UnityEngine;

public class MahjongAssets : ScriptableObject
{
    public float width;
    public float height;
    public float offset;
    public float throwTime;
    public float moveTime;
    public float smoothTime;
    public float maxSpeed;
    public Vector3 handPosition;
    public Vector3 discardPosition;
    public Vector3 scorePosition;
    public Vector3 groupPosition;
    public MahjongAsset asset;
    public Texture[] textures;
    private MahjongAsset __asset;
    private float __groupOffset;

    public MahjongAsset Create()
    {
        return Instantiate(asset);
    }

    public MahjongAsset Create(Mahjong.Tile tile)
    {
        MahjongAsset result = Instantiate(asset);
        if (textures != null)
        {
            byte code = tile;
            if (textures.Length > code)
            {
                Material material = result == null || result.renderer == null ? null : result.renderer.material;
                if (material != null)
                    material.mainTexture = textures[code];
            }
        }

        return result;
    }

    public void Discard(MahjongAsset asset, int index)
    {
        Transform transform = asset == null ? null : asset.transform;
        if (transform == null)
            return;

        transform.localEulerAngles = new Vector3(90.0f, 0.0f, 0.0f);
        transform.localPosition = discardPosition + new Vector3(width * (index % 5), 0.0f, -height * (index / 5));

        asset.Throw();

        __asset = asset;
    }

    public void Score(MahjongAsset asset, int index)
    {
        Transform transform = asset == null ? null : asset.transform;
        if (transform == null)
            return;

        transform.localEulerAngles = new Vector3(90.0f, 0.0f, 0.0f);
        transform.localPosition = scorePosition + new Vector3(width * index, 0.0f, 0.0f);

        asset.Throw();
    }

    public void Group(MahjongAsset x, MahjongAsset y, MahjongAsset z)
    {
        __groupOffset += offset;

        Transform transform = __asset == null ? null : __asset.transform;
        if (transform != null)
            transform.localPosition = groupPosition + new Vector3(-(__groupOffset += width), 0.0f, 0.0f);

        transform = x == null ? null : x.transform;
        if (transform != null)
        {
            transform.localEulerAngles = new Vector3(90.0f, 0.0f, 0.0f);
            transform.localPosition = groupPosition + new Vector3(-(__groupOffset += width), 0.0f, 0.0f);
        }

        transform = y == null ? null : y.transform;
        if (transform != null)
        {
            transform.localEulerAngles = new Vector3(90.0f, 0.0f, 0.0f);
            transform.localPosition = groupPosition + new Vector3(-(__groupOffset += width), 0.0f, 0.0f);
        }

        transform = z == null ? null : z.transform;
        if (transform != null)
        {
            transform.localEulerAngles = new Vector3(90.0f, 0.0f, 0.0f);
            transform.localPosition = groupPosition + new Vector3(-(__groupOffset += width), 0.0f, 0.0f);
        }
    }
}
