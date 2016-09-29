/**
 * Kerbal Visual Enhancements is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * Kerbal Visual Enhancements is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program. If not, see <http://www.gnu.org/licenses/>.
 *
 * Copyright © 2013-2016 Ryan Bray, RangeMachine
 */

using UnityEngine;

namespace Geometry
{
    public static class Quad
    {
        public static void Create(GameObject gameObject, float size, Color color, Vector3 up)
        {
            MeshFilter filter = gameObject.AddComponent<MeshFilter>();
            Mesh mesh = filter.mesh;
            mesh.Clear();
            
            mesh.vertices = new Vector3[4] 
            {
                new Vector3(-.5f,-.5f,0)*size,
                new Vector3(-.5f,.5f,0)*size,
                new Vector3(.5f,-.5f,0)*size,
                new Vector3(.5f,.5f,0)*size
            };

            mesh.triangles = new int[] { 1, 3, 2, 2, 0, 1 };

            mesh.uv = new Vector2[4] 
            { 
                new Vector2(0,0),
                new Vector2(0,1),
                new Vector2(1,0),
                new Vector2(1,1)
            };

            mesh.normals = new Vector3[4]
            {
                up,
                up,
                up,
                up
            };

            mesh.colors = new Color[4]
            {
                new Color(color.r, color.g, color.b, color.a),
                new Color(color.r, color.g, color.b, color.a),
                new Color(color.r, color.g, color.b, color.a),
                new Color(color.r, color.g, color.b, color.a)
            };

            Tools.CalculateMeshTangents(mesh);

            mesh.RecalculateBounds();
            mesh.Optimize();
        }

    }
}
