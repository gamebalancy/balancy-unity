#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Balancy
{
    public class EditorUtils
    {
        public static List<Texture> GetAllTexturesFromMaterial(Renderer renderer)
        {
            List<Texture> allTexture = new List<Texture>();
            Shader shader = renderer.sharedMaterial.shader;
            for (int i = 0; i < ShaderUtil.GetPropertyCount(shader); i++)
            {
                if (ShaderUtil.GetPropertyType(shader, i) == ShaderUtil.ShaderPropertyType.TexEnv)
                {
                    Texture texture = renderer.sharedMaterial.GetTexture(ShaderUtil.GetPropertyName(shader, i));
                    allTexture.Add(texture);
                }
            }

            return allTexture;
        }
    }
}
#endif