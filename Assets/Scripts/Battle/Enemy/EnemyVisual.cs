using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class EnemyVisual : MonoBehaviour
{
    [Header("外观设置")]
    [SerializeField] private Color _color = new Color(1f, 0.2f, 0.2f); // 红色
    [SerializeField] private int _textureSize = 64;
    [SerializeField, Range(0f, 1f)] private float _radiusRatio = 0.8f;

    void Awake()
    {
        GenerateSprite();
    }

    private void GenerateSprite()
    {
        Texture2D texture = new Texture2D(_textureSize, _textureSize);
        Color transparent = Color.clear;
        Color border = new Color(_color.r * 0.6f, _color.g * 0.6f, _color.b * 0.6f, 1f);

        float center = (_textureSize - 1) / 2f;
        float radius = center * _radiusRatio;
        float innerRadius = radius * 0.85f;

        for (int y = 0; y < _textureSize; y++)
        {
            for (int x = 0; x < _textureSize; x++)
            {
                float dx = x - center;
                float dy = y - center;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);

                if (dist <= radius)
                {
                    if (dist > innerRadius)
                        texture.SetPixel(x, y, border);
                    else
                        texture.SetPixel(x, y, _color);
                }
                else
                {
                    texture.SetPixel(x, y, transparent);
                }
            }
        }

        texture.Apply();

        Sprite sprite = Sprite.Create(
            texture,
            new Rect(0, 0, _textureSize, _textureSize),
            new Vector2(0.5f, 0.5f),
            100f
        );

        GetComponent<SpriteRenderer>().sprite = sprite;
    }
}
