using System;
using UnityEngine;
using UnityEngine.EventSystems;

public class MahjongAsset : MonoBehaviour, IPointerClickHandler, IPointerDownHandler, IPointerUpHandler
{
    public Action onSelected;
    public Action onDiscard;

    public new Renderer renderer;
    public float sqrDistance;
    public bool isPlayAnimation;
    private bool __isSelected;
    private float __depth;
    private Vector3 __position;
    private Vector3 __offset;
    private PointerEventData __pointerEventData;

    public bool isSelected
    {
        get
        {
            return __isSelected;
        }

        private set
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

    public bool isDraging
    {
        get
        {
            return __pointerEventData != null;
        }
    }

    public Vector3 position
    {
        get
        {
            Camera camera = __pointerEventData == null ? null : __pointerEventData.pressEventCamera;
            return camera == null ? Vector3.zero : camera.ScreenToWorldPoint(__position - __offset);
        }

        set
        {
            Camera camera = __pointerEventData == null ? null : __pointerEventData.pressEventCamera;
            if (camera != null)
                __position = camera.WorldToScreenPoint(value) + __offset;
        }
    }

    public void Visible()
    {
        transform.localEulerAngles = new Vector3(90.0f, 0.0f, 0.0f);
    }

    public void Hide()
    {
        transform.localEulerAngles = Vector3.zero;
    }
    
    public void Move()
    {
        Vector3 position = transform.localPosition;
        
        position.y += 5.0f;

        transform.localPosition = position;

        Invoke("__OnMove", 3.0f);
    }

    public void Throw()
    {
        Visible();
    }

    private void __OnMove()
    {
        Vector3 position = transform.localPosition;

        position.y -= 5.0f;

        transform.localPosition = position;

    }
    
    void Update()
    {
        if (onDiscard == null)
        {
            /*if(__pointerEventData != null)
                ((IPointerUpHandler)this).OnPointerUp(__pointerEventData);*/

            __pointerEventData = null;
        }
        else if (__pointerEventData != null)
        {
            if (onSelected == null)
            {
                Camera camera = __pointerEventData.pressEventCamera;
                if (camera != null)
                {
                    Vector3 position = __pointerEventData.position;
                    position.z = __depth;
                    position -= __offset;
                    transform.position = camera.ScreenToWorldPoint(position);
                }
            }
            else
                ((IPointerUpHandler)this).OnPointerUp(__pointerEventData);
        }

        isSelected = onSelected != null;
    }

    void IPointerClickHandler.OnPointerClick(PointerEventData eventData)
    {
        if(onSelected == null || __pointerEventData != null)
            return;
        
        if (onSelected != null)
            onSelected();
    }
    
    void IPointerDownHandler.OnPointerDown(PointerEventData eventData)
    {
        if (eventData == null || onDiscard == null || onSelected != null)
            return;

        Camera camera = eventData.pressEventCamera;
        Transform transform = camera == null ? null : camera.transform;
        if (transform != null)
        {
            __depth = Vector3.Distance(transform.position, this.transform.position);
            __position = eventData.pressPosition;
            __position.z = __depth;
            __offset = __position - camera.WorldToScreenPoint(this.transform.position);

            __pointerEventData = eventData;
        }
    }

    void IPointerUpHandler.OnPointerUp(PointerEventData eventData)
    {
        if (__pointerEventData == null)
            return;

        if (onDiscard != null && (__pointerEventData.position - (Vector2)__pointerEventData.pressEventCamera.WorldToScreenPoint(__position)).sqrMagnitude > sqrDistance)
            onDiscard();

        Camera camera = __pointerEventData.pressEventCamera;
        if (camera != null)
            transform.position = camera.ScreenToWorldPoint(__position - __offset);

        __pointerEventData = null;
    }
}
