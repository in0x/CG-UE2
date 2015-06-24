using System;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;

using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;

namespace Exercise2
{
    public class ObjMeshLoader
    {
        public static bool Load(ObjMesh mesh, string fileName)
        {
            try
            {
                using (StreamReader streamReader = new StreamReader(fileName))
                {
                    Load(mesh, streamReader);
                    streamReader.Close();
                    return true;
                }
            }
            catch (FormatException e)
            {
                Debug.WriteLine(e.Message);
                Debug.WriteLine(e.Data);
                Debug.WriteLine(e.StackTrace);
                return false;
            }
            catch
            { 
                return false; 
            }
        }

        static char[] splitCharacters = new char[] { ' ' };

        static List<Vector3> vertices;
        static List<Vector3> normals;
        static List<Vector2> texCoords;
        static Dictionary<ObjMesh.ObjVertex, int> objVerticesIndexDictionary;
        static List<ObjMesh.ObjVertex> objVertices;
        static List<ObjMesh.ObjTriangle> objTriangles;

        static void Load(ObjMesh mesh, TextReader textReader)
        {
            vertices = new List<Vector3>();
            normals = new List<Vector3>();
            texCoords = new List<Vector2>();
            objVerticesIndexDictionary = new Dictionary<ObjMesh.ObjVertex, int>();
            objVertices = new List<ObjMesh.ObjVertex>();
            objTriangles = new List<ObjMesh.ObjTriangle>();

            string line;
            while ((line = textReader.ReadLine()) != null)
            {
                line = line.Trim(splitCharacters);
                line = line.Replace("  ", " ");

                string[] parameters = line.Split(splitCharacters);

                switch (parameters[0])
                {
                    case "p": // Point
                        break;

                    case "v": // Vertex
                        float x = float.Parse(parameters[1], System.Globalization.CultureInfo.InvariantCulture);
                        float y = float.Parse(parameters[2], System.Globalization.CultureInfo.InvariantCulture);
                        float z = float.Parse(parameters[3], System.Globalization.CultureInfo.InvariantCulture);
                        vertices.Add(new Vector3(x, y, z));
                        break;

                    case "vt": // TexCoord
                        float u = float.Parse(parameters[1], System.Globalization.CultureInfo.InvariantCulture);
                        float v = float.Parse(parameters[2], System.Globalization.CultureInfo.InvariantCulture);
                        texCoords.Add(new Vector2(u, v));
                        break;

                    case "vn": // Normal
                        float nx = float.Parse(parameters[1], System.Globalization.CultureInfo.InvariantCulture);
                        float ny = float.Parse(parameters[2], System.Globalization.CultureInfo.InvariantCulture);
                        float nz = float.Parse(parameters[3], System.Globalization.CultureInfo.InvariantCulture);
                        normals.Add(new Vector3(nx, ny, nz));
                        break;

                    case "f":
                        switch (parameters.Length)
                        {
                            case 4:
                                ObjMesh.ObjTriangle objTriangle = new ObjMesh.ObjTriangle();
                                objTriangle.Index0 = ParseFaceParameter(parameters[1]);
                                objTriangle.Index1 = ParseFaceParameter(parameters[2]);
                                objTriangle.Index2 = ParseFaceParameter(parameters[3]);
                                objTriangles.Add(objTriangle);
                                break;

                            case 5:
                                throw new NotSupportedException("Quads not supported");
                        }
                        break;
                }
            }

            mesh.Vertices = objVertices.ToArray();
            mesh.Triangles = objTriangles.ToArray();

            objVerticesIndexDictionary = null;
            vertices = null;
            normals = null;
            texCoords = null;
            objVertices = null;
            objTriangles = null;
        }

        static char[] faceParamaterSplitter = new char[] { '/' };
        static int ParseFaceParameter(string faceParameter)
        {
            Vector3 vertex = new Vector3();
            Vector2 texCoord = new Vector2();
            Vector3 normal = new Vector3();

            string[] parameters = faceParameter.Split(faceParamaterSplitter);

            int vertexIndex = int.Parse(parameters[0]);
            if (vertexIndex < 0) vertexIndex = vertices.Count + vertexIndex;
            else vertexIndex = vertexIndex - 1;
            vertex = vertices[vertexIndex];

            if (parameters.Length > 1 && parameters[1].Length > 0)
            {
                int texCoordIndex = int.Parse(parameters[1]);
                if (texCoordIndex < 0) texCoordIndex = texCoords.Count + texCoordIndex;
                else texCoordIndex = texCoordIndex - 1;
                texCoord = texCoords[texCoordIndex];
            }

            if (parameters.Length > 2 && parameters[2].Length > 0)
            {
                int normalIndex = int.Parse(parameters[2]);
                if (normalIndex < 0) normalIndex = normals.Count + normalIndex;
                else normalIndex = normalIndex - 1;
                normal = normals[normalIndex];
            }

            return FindOrAddObjVertex(ref vertex, ref texCoord, ref normal);
        }

        static int FindOrAddObjVertex(ref Vector3 vertex, ref Vector2 texCoord, ref Vector3 normal)
        {
            ObjMesh.ObjVertex newObjVertex = new ObjMesh.ObjVertex();
            newObjVertex.Vertex = vertex;
            newObjVertex.TexCoord = texCoord;
            newObjVertex.Normal = normal;

            int index;
            if (objVerticesIndexDictionary.TryGetValue(newObjVertex, out index))
            {
                return index;
            }
            else
            {
                objVertices.Add(newObjVertex);
                objVerticesIndexDictionary[newObjVertex] = objVertices.Count - 1;
                return objVertices.Count - 1;
            }
        }
    }

}