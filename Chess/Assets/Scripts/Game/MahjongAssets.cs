using UnityEngine;

public class MahjongAssets : ScriptableObject
{
    public float width;
    public float height;
    public float length;
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

    public void Group(MahjongAsset asset, int groupIndex, int index)
    {
        Transform transform = asset == null ? null : asset.transform;
        if (transform == null)
            return;
        
        transform.localEulerAngles = new Vector3(90.0f, index > 2 ? 90.0f : 0.0f, 0.0f);
        transform.localPosition = groupPosition - new Vector3((groupIndex * 3 + (index > 2 ? 1 : index)) * width + groupIndex * offset, index > 2 ? length : 0.0f, 0.0f);
    }
}
