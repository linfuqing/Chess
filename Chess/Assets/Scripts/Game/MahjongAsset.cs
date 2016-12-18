using System;
using UnityEngine;
using UnityEngine.EventSystems;

public class MahjongAsset : MonoBehaviour, IPointerClickHandler, IEndDragHandler
{
    public event Action onSelected;
    public event Action onDiscard;

    public new Renderer renderer;
    public bool isPlayAnimation;
    private bool __isSelected;

    public bool isSelected
    {
        get
        {
            return __isSelected;
        }

        set
        {
            if (__isSelected == value)
                return;

            Vector3 position = transform.localPosition;

            if (value)
                position.y += 2.5f;
            else
                position.y -= 2.5f;

            transform.localPosition = position;

            __isSelected = value;
        }
    }
    
    public void Move()
    {
        Vector3 position = transform.localPosition;
        
        position.y += 5.0f;

        transform.localPosition = position;

        Invoke("__OnMove", 3.0f);
    }

    public void Discard()
    {

    }
    
    private void __OnMove()
    {
        Vector3 position = transform.localPosition;

        position.y -= 5.0f;

        transform.localPosition = position;

    }

    void IPointerClickHandler.OnPointerClick(PointerEventData eventData)
    {
        if (onSelected != null)
            onSelected();
    }

    void IEndDragHandler.OnEndDrag(PointerEventData eventData)
    {
        if (onDiscard != null)
            onDiscard();
    }
}
