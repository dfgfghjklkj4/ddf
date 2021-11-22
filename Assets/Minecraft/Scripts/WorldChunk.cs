using MyNamespace;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Compression;
using System.Linq;
//using System.Linq;
//using System.Numerics;
using System.Security;
using System.Security.Permissions;
using UnityEngine;
using UnityEngine.Rendering;
using static UnityEngine.Mesh;
using Debug = UnityEngine.Debug;
using Random = UnityEngine.Random;






public class treeDate
{
    public int treeID;
    public Dictionary<Vector3, BlockType> treeDic;
}
public struct PerlinNoisePreComputeDate
{
    public int[,] hightMap;
    public bool isRavine;
}
public struct PerlinNoisePreCompute
{
    public static int waterLevel=68;
    // public int ironDepth = 5;
    public int baseNoise;
    public float ridgeMask;
   public PerlinNoisePreComputeDate date;
    //  public bool isRavine;
    //public  bool isInnerRavine;


    public float p1;
   // public int[,] hightMap;
    public int maxHight, minHight;

    public void PerlinCompute(int x, int z)
    {
        if (date.hightMap == null)
        {
            date.hightMap = new int[ChunkManager.Instance.chunkSize.x, ChunkManager.Instance.chunkSize.z];

        }
       
        int x0, z0;
        for (int x1 = 0; x1 < WorldChunk._size.x; x1++)
        {
            x0 = x + x1;
            for (int z1 = 0; z1 < WorldChunk._size.z; z1++)
            {
                z0 = z + z1;
              

                float ox = x0 /100f + ChunkManager.offset.x;
                float oz = z0 / 100f + ChunkManager.offset.y;

                float p0 = Mathf.PerlinNoise(ox, oz)*1.5f;
                p0 = Mathf.Pow(p0, 1.5f);
                if (float.IsNaN(p0) || p0 < 0)
               {
                   p0 = 0f;
                }

                baseNoise = Mathf.FloorToInt(p0 * 20);
                baseNoise += 63;



                //山脉
                ridgeMask = Mathf.PerlinNoise((x0 + ChunkManager.offset.x) / 64f, (z0 + ChunkManager.offset.y) / 64f)*1f;

              
                if (ridgeMask < 0.3f)
                {
                    //  ChunkManager.Instance.c1++;
                    //河道宽度
                    float ridgeNoise = WorldChunk.RidgeNoise(x0 / 30f, z0 / 30f);
                    bool isInnerRavine = ridgeNoise < 0.3f && baseNoise > waterLevel + 2;
                    date.isRavine = ridgeNoise < 0.4f && baseNoise > waterLevel + 2;

                    if (isInnerRavine)
                    {
                        //   noise -= 16;
                        baseNoise -= 16;
                    }
                    else if (date.isRavine)
                    {
                        // noise -= Mathf.RoundToInt(16 * (1 - Mathf.InverseLerp(0.1f, 0.15f, ridgeNoise)));
                        baseNoise -= Mathf.RoundToInt(16 * (1 - Mathf.InverseLerp(0.1f, 0.15f, ridgeNoise)));
                    }
                }


                if (baseNoise > maxHight)
                    maxHight = baseNoise;
                date.hightMap[x1, z1] = baseNoise;




            }
        }





        return;
    }





}



public class WorldChunk : MonoBehaviour
{
    public Vector3Int ID;
    public List<int> treeslistINDEX = new List<int>();
    public bool isAir;

    public WorldChunk NeighborUp, NeighborDown, NeighborLeft, NeighborRight, NeighborForward, NeighborBack;


    //   public bool IsModified;


    public float dis;//离摄像机距离；
    public bool highlight;
    public bool Initialized;


    public bool computedTerrainDate;

    public bool isEmperty = false;

    public bool active;

    public bool buildMesh;
    public bool IsLoaded;


    public MeshFilter OpaqueMeshFilter, WaterMeshFilter, FoliageMeshFilter, TreeMeshFilter;
    public MeshRenderer OpaqueMeshRenderer, WaterMeshRenderer, FoliageMeshRenderer, TreeMeshRenderer;
    public MeshCollider OpaqueMeshCol, WaterMeshCol, FoliageMeshCol,TreeMeshCol;
    public BlockType[,,] Blocks;


    public int[,] hightmap;

    public int indexOfExternalBlockArray;

    public int meshesIndex;

    public MeshData_ coubinMD;
    public static bool[] visibility = new bool[6];
    public static Vector3Int[] neighborsArray = new Vector3Int[6];
    public static bool[] neighborsBlock = new bool[6];



    public static Bounds bound;
 
    public static Vector3 boundCenter;
    public static Vector3Int _size;
    public static Vector3Int _sizeSmallOne;
 
    public int _seed;
    public HashSet<Vector3Int> AddBlocks;
    public HashSet<Vector3Int> TouchedBlocks;
    public List<Vector3Int> DestroyBlocks;


    public PerlinNoisePreCompute[,] highMap;
    public static Queue<PerlinNoisePreCompute[,]> highMapPool = new Queue<PerlinNoisePreCompute[,]>(512);

    public static Queue<PerlinNoisePreCompute> PerlinDatePool = new Queue<PerlinNoisePreCompute>(1024);

    public static Dictionary<Vector2Int, PerlinNoisePreCompute> hightMapDic = new Dictionary<Vector2Int, PerlinNoisePreCompute>(1024);


    // static Vector3Int[] tempmd = new Vector3Int[256];


    public void UnUseSet()
    {
        if (removeFlag == false)
        {
            //    return;
        }
        removeFlag = false;
        if (!isAir)
        {
            LOD = -1;
            haveTree = false;
            for (int i = 0; i < treeslistINDEX.Count; i++)
            {
                ObjPool.ReturnGO<Transform>(treeslist2[i], ChunkManager.Instance.trees[treeslistINDEX[i]]);
             
            }
            treeslist2.Clear();
            treeslistINDEX.Clear();
            if (OpaqueMeshFilter.sharedMesh.vertexCount > 0)
            {
                OpaqueMeshCol.enabled = false;
                OpaqueMeshRenderer.renderingLayerMask = 0;
                OpaqueMeshFilter.sharedMesh.Clear();
            }
            if (WaterMeshFilter.sharedMesh.vertexCount > 0)
            {
                WaterMeshCol.enabled = false;
                WaterMeshRenderer.renderingLayerMask = 0;
                WaterMeshFilter.sharedMesh.Clear();
            }
            if (FoliageMeshFilter.sharedMesh.vertexCount > 0)
            {
                FoliageMeshCol.enabled = false;
                FoliageMeshRenderer.renderingLayerMask = 0;
                FoliageMeshFilter.sharedMesh.Clear();
            }
     
            Initialized = false;

           
            computedTerrainDate = false;


            isEmperty = true;

            //  active = false;

            buildMesh = false;
            IsLoaded = false;
            RemoveNeighbors();
            pool.Push(this);
            ChunkManager._chunks.Remove(ID);
        }


    }

    bool havegrass;
    void RemoveNeighbors()
    {
        if (NeighborUp)
        {
            NeighborUp.NeighborDown = null;
            NeighborUp = null;
        }
        if (NeighborDown)
        {
            NeighborDown.NeighborUp = null;
            NeighborDown = null;
        }
        if (NeighborLeft)
        {
            NeighborLeft.NeighborRight = null;
            NeighborLeft = null;
        }
        if (NeighborRight)
        {
            NeighborRight.NeighborLeft = null;
            NeighborRight = null;
        }
        if (NeighborForward)
        {
            NeighborForward.NeighborBack = null;
            NeighborForward = null;
        }
        if (NeighborBack)
        {
            NeighborBack.NeighborForward = null;
            NeighborBack = null;
        }
    }
    public bool outView=false;
    public void StartBuildMesh()
    {

        if (isEmperty)
            return;
        //六面邻居的地形数据加载
        loadNeighborsBlocks();
   
     BuildOpaqueMesh();
    BuildWaterMesh();
   BuildFoliageMesh();
   
        buildMesh = true;
     

    }
    public bool IsRebuildOpaqueMesh, IsRebuildWaterMesh, IsRebuildFoliageMesh;
    public bool haveTree;
    public void ReBuildMesh()
    {

        if (isEmperty)
            return;
        //六面邻居的地形数据加载
        loadNeighborsBlocks();

        //  if(IsRebuildOpaqueMesh)
        BuildOpaqueMesh();
     //   IsRebuildOpaqueMesh = false;
      //    if (IsRebuildWaterMesh)
     BuildWaterMesh();
     //   IsRebuildWaterMesh = false;
        //  if (IsRebuildFoliageMesh)
   BuildFoliageMesh();
  //      BuildTreeMesh();
        //IsRebuildFoliageMesh = true;


      //   if (IsLoaded)
      //   gameObject.SetActive(true);

      buildMesh = true;


    }

    public void Initialize(Vector3Int minCorner)
    {

     
        
        if (Initialized)

            return;


        ID = minCorner;

        GetLOD();

        this.transform.position = minCorner;

        isEmperty = false;
        if (AddBlocks==null)
        {
            AddBlocks = new HashSet<Vector3Int>();
            TouchedBlocks = new HashSet<Vector3Int>();
            DestroyBlocks = new List<Vector3Int>(512);
        }

        if (!active)
        {

           

            Active();

        }

        //  ModifyBlock=GetMBList();
        //  ModifyPos = GetMBPosList(out mbIndex);

        if (Blocks == null)
        {

            Blocks = new BlockType[ChunkManager.Instance.chunkSize.x, ChunkManager.Instance.chunkSize.y, ChunkManager.Instance.chunkSize.z];

        }
        if (hightmap == null)
        {

            hightmap = new int[ChunkManager.Instance.chunkSize.x, ChunkManager.Instance.chunkSize.z];

        }
        //  Blocks = new BlockType[_size.x, _size.y, _size.z];

        Initialized = true;

        Vector3Int wp = new Vector3Int(minCorner.x, minCorner.y + ChunkManager.Instance.chunkSize.y, minCorner.z);
        // /*
        if (!NeighborUp)
        {
            if (minCorner.y < ChunkManager.maxHight - ChunkManager.Instance.chunkSize.y)
            {


                if (!ChunkManager._chunks.TryGetValue(wp, out NeighborUp))
                {
                    NeighborUp = WorldChunk.GetChunk();
                    NeighborUp.Initialize(wp);

                }

            }
            else
            {
                NeighborUp = ChunkManager.AirChunk;
            }
        }

        NeighborUp.NeighborDown = this;


        //  if (!ChunkManager._chunks.ContainsKey(minCorner))
        //  {
        ChunkManager._chunks.Add(minCorner, this);
        // }


        return;
    }


    public void GetLOD()
    {
     
       // Vector2 v2 = new Vector2(ID.x, ID.z);
        Vector2 playerChunk = new Vector2(ChunkManager.Instance.currentChunkPos.x, ChunkManager.Instance.currentChunkPos.z);
        float xdis = Mathf.Abs(ID.x - ChunkManager.Instance.currentChunkPos.x) / ChunkManager.Instance.chunkSize.x;
        float zdis = Mathf.Abs(ID.z - ChunkManager.Instance.currentChunkPos.z) / ChunkManager.Instance.chunkSize.z;
        if (xdis <= 0.001f && zdis <= 0.001f)
        {
            LOD = 0;
        }
        else if (xdis <= 1.001f && zdis <= 1.001f || zdis <= 1.001f && xdis <= 1.001f)
        {
            LOD = 0;
        }
        else if (xdis <= 2.001f && zdis <= 2.001f || zdis <= 2.001f && xdis <= 2.001f)
        {
            LOD = 1;
        }
        //    else if (xdis <= 3.001f && zdis <= 3.001f || zdis <= 3.001f && xdis <= 3.001f)
        ///  {
        //      LOD = 3;
        //   }
        else
        {
            LOD = 2;
        }
    }

    public void InitializeNeighbors()
    {

        Vector3Int wp;

        if (!NeighborDown)
        {
            wp = new Vector3Int(ID.x, ID.y - ChunkManager.Instance.chunkSize.y, ID.z);
            if (!ChunkManager._chunks.TryGetValue(wp, out NeighborDown))
            {
                NeighborDown = WorldChunk.GetChunk();
                NeighborDown.Initialize(wp);
            }

        }
        NeighborDown.NeighborUp = this;
        //    */


        if (!NeighborForward)
        {
            wp = new Vector3Int(ID.x, ID.y, ID.z + ChunkManager.Instance.chunkSize.z);
            if (!ChunkManager._chunks.TryGetValue(wp, out NeighborForward))
            {
                NeighborForward = WorldChunk.GetChunk();
                NeighborForward.Initialize(wp);
            }

        }
        NeighborForward.NeighborBack = this;
        if (!NeighborBack)
        {
            wp = new Vector3Int(ID.x, ID.y, ID.z - ChunkManager.Instance.chunkSize.z);
            if (!ChunkManager._chunks.TryGetValue(wp, out NeighborBack))
            {

                NeighborBack = WorldChunk.GetChunk();
                NeighborBack.Initialize(wp);
            }

        }
        NeighborBack.NeighborForward = this;
        if (!NeighborLeft)
        {
            wp = new Vector3Int(ID.x - ChunkManager.Instance.chunkSize.x, ID.y, ID.z);
            if (!ChunkManager._chunks.TryGetValue(wp, out NeighborLeft))
            {
                NeighborLeft = WorldChunk.GetChunk();
                NeighborLeft.Initialize(wp);
            }

        }
        NeighborLeft.NeighborRight = this;

        if (!NeighborRight)
        {
            wp = new Vector3Int(ID.x + ChunkManager.Instance.chunkSize.x, ID.y, ID.z);
            if (!ChunkManager._chunks.TryGetValue(wp, out NeighborRight))
            {
                NeighborRight = WorldChunk.GetChunk();
                NeighborRight.Initialize(wp);
            }

        }
        NeighborRight.NeighborLeft = this;

        return;
    }

    static int ccccc = 0;


    //0.0005

    public void CreateTerrainDate()
    {
        
        if (computedTerrainDate)

            return;

       
     

        Vector2Int mapid = new Vector2Int(ID.x, ID.z);
        PerlinNoisePreCompute pnpc;
        if (!hightMapDic.TryGetValue(mapid, out pnpc))
        {
            if (PerlinDatePool.Count > 0)
            {
                pnpc = PerlinDatePool.Dequeue();
            }
            else
            {
                pnpc = new PerlinNoisePreCompute();
              
            }

            pnpc.PerlinCompute(ID.x, ID.z);

            hightMapDic.Add(mapid, pnpc);
        }
        else
        {

        }
       // /*
          //  if (pnpc.maxHight < ID.y-12)
         //  if (!NeighborUp.isAir&& NeighborUp.isEmperty)
          if (false)
            {
         //   fullShutoutDown = false;
          isEmperty = true;
            computedTerrainDate = true;
            for (int x = 0; x < ChunkManager.Instance.chunkSize.x; x++)
            {
                for (int y = 0; y < ChunkManager.Instance.chunkSize.y; y++)
                {
                    for (int z = 0; z < ChunkManager.Instance.chunkSize.z; z++)
                    {
                        Blocks[x, y, z] = BlockType.Air;
                    }
                }
            }


        //    Debug.Log(pnpc.maxHight+"  "+ID);
            return;
        }
      //   */

        bool eee = false;
        isEmperty = true;
        for (int x = 0; x < _size.x; x++)
        {
            int wpx = x + ID.x;
            for (int z = 0; z < _size.z; z++)
            {
                int wpz = z + ID.z;




                int h = 0;
                for (int y = _sizeSmallOne.y; y > -1; y--)
                //for (int y = 0; y < _size.y; y++)
                {
                    // float t = Time.realtimeSinceStartup; 
                    BlockType type = GetBlockType(x, y + ID.y, z, _seed, pnpc);
                    //  BlockType type = GetBlockType(wpx, y + ID.y, wpz, _seed);
                    //   t = Time.realtimeSinceStartup - t;
                   if (!type.isTransparent)
                   
                    {


                        if (!eee)
                        {
                            h = y;
                            eee = true;
                        }


                    }
                     if (!type.isAir)
                    {
                     
                        if (isEmperty)
                            isEmperty = false;
                    }

                    // if (type.blockName!=BlockNameEnum.Air)
                    // var t = Blocks[x, y, z];
                    //   if (t==null||!t.isTree)
                    // {
                    Blocks[x, y, z] = type;
                  //  }
                   

                }
                if (eee)
                {
                    //发现了不透明的方块 
                    eee = false;
                    hightmap[x, z] = h;
                   // if (isEmperty)
                   //     isEmperty = false;
                    // h = 0;

                }
                else
                {
                    hightmap[x, z] = 0;
                    //如果整个竖排从下到上都没有不透明的方块 就不能完全遮住下面的chunk
                 

                }

             
            }
        }
      
        computedTerrainDate = true;

   
    }




    public void Active()
    {
        if (!active)
        {
            AddBlocks = new HashSet<Vector3Int>();
            TouchedBlocks = new HashSet<Vector3Int>();
            DestroyBlocks = new List<Vector3Int>();



            // gameObject.name = ID.ToString();
            Mesh m1 = new Mesh();
            OpaqueMeshFilter.sharedMesh = m1;
            //    dataArray = Mesh.AllocateWritableMeshData(1);
            // data = dataArray[0];
            OpaqueMeshCol.sharedMesh = OpaqueMeshFilter.sharedMesh;

            Mesh m2 = new Mesh();
            WaterMeshFilter.sharedMesh = m2;
            // WaterMeshCol = WaterMeshFilter.GetComponent<MeshCollider>();
            WaterMeshCol.sharedMesh = WaterMeshFilter.sharedMesh;
            
            Mesh m3 = new Mesh();
            FoliageMeshFilter.sharedMesh = m3;
            //  FoliageMeshCol = FoliageMeshFilter.GetComponent<MeshCollider>();
            FoliageMeshCol.sharedMesh = FoliageMeshFilter.sharedMesh;

         //   Mesh m4 = new Mesh();
          //  TreeMeshFilter.sharedMesh = m4;
         // TreeMeshCol.sharedMesh = TreeMeshFilter.sharedMesh;

            active = true;
        }

    }



 
    public static float RidgeNoise(float x, float y)
    {
        return Mathf.Abs(Mathf.PerlinNoise(x, y) - 0.5f) * 2.0f;
    }

    BlockType GetBlockType(int x, int y, int z, int seed, PerlinNoisePreCompute pnpc)
    {

 
        int baseNoise = pnpc.date.hightMap[x, z];

        BlockType type = BlockType.Air;
        int waterLevel = PerlinNoisePreCompute.waterLevel;
        // int waterLevel = 65;
        int ironDepth = 5;

        //山脉
        //  float ridgeMask = pnpc.ridgeMask;

        int noise = baseNoise;

        bool isLake = baseNoise <= waterLevel;
        bool isRavine = false;
      

        //  ChunkManager.Instance.c1++;
        if (y <= 0)
        {

            // type.blockName = BlockNameEnum.Bedrock;
            type = BlockType.Bedrock;
        }
        else if (y >= noise - 8 && y <= noise && isLake && isRavine == false)
        {
            //     ChunkManager.Instance.c2++;

            type = (y >= noise - 3) ? BlockType.Sand : BlockType.Sandstone;
        }
        else if (y < baseNoise - 3 || (isRavine && y < baseNoise && y < noise))
        {
            //   ChunkManager.Instance.c3++;
            type = BlockType.Stone;
          //  return type;
            if (y < noise)
            {
                //  ChunkManager.Instance.c3++;
                //  type = BlockType.Air;

                if (y < baseNoise - 35 && Random.value < 0.001f)
                {
                    // ChunkManager.Instance.c1++;
                    type = BlockType.Diamond_Ore;
                }

                else if (type == BlockType.Air && y <= baseNoise - 6)
                {

                    // ChunkManager.Instance.c2++;
                    //float p1 = Mathf.PerlinNoise((x + ChunkManager.offset.x) / 6f + 0.5f, (z + ChunkManager.offset.y) / 6f + 0.5f);
                    float p1 = pnpc.p1;

                    float p2 = Mathf.PerlinNoise(y / 6f, 0);
                    float p3 = p1 + p2;
                    if (p3 > 1.3f)
                    {
                        type = BlockType.Gravel;
                        // GameObject.Instantiate(chunkManager.yellow, new Vector3(x, y, z), Quaternion.identity).transform.SetSiblingIndex(0);
                    }
                }

                else if (type == BlockType.Air && y <= baseNoise - ironDepth)
                {
                    //  ChunkManager.Instance.c3++;
                    //  ChunkManager.Instance.c1++;
                    float p1 = Mathf.PerlinNoise((x + ChunkManager.offset.x) / 4f + 100, (z + ChunkManager.offset.y) / 4f + 100);
                    //  float p1 = pnpc.pp1;
                    float p2 = Mathf.PerlinNoise(y / 4f, 0);

                    float p3 = p1 + p2;
                    if (p3 > 1.4f)
                    {
                        //    GameObject.Instantiate(chunkManager.blue, new Vector3(x, y, z), Quaternion.identity).transform.SetSiblingIndex(0);
                        //  ChunkManager.Instance.c4++;
                        type = BlockType.Iron_Ore;
                    }

                }
                if (type == BlockType.Air)

                    type = BlockType.Stone;

                // type = type ?? BlockType.GetBlockType(BlockNameEnum.Stone);
            }


        }

        else if (isLake && y <= waterLevel && isRavine == false)
        {
            //  ChunkManager.Instance.c4++;

            type = BlockType.Water;
        }

        else if (y < noise)
        {
            type = BlockType.Dirt;
        }

        else if (y == noise && y > waterLevel)
        {
            type = BlockType.Grass;
        }
        else if (y == noise + 1 && y > waterLevel + 1 && isRavine == false)
        {
            //   ChunkManager.Instance.c1++;
            //   Instantiate<GameObject>(ChunkManager.Instance.gp,new Vector3(ID.x+x,y,ID.z+z),Quaternion.identity);

            float plantProbability = 1f - Mathf.InverseLerp(waterLevel, waterLevel + 30, y);
            plantProbability = Mathf.Pow(plantProbability, 7f);
          // plantProbability *= 1.25f;

            if (Random.value < plantProbability)
            {

                float p = Random.value;

              type = BlockType.GetTallGrassBlock();
           //  treePosDic2.Add(new Vector3Int(x, y - ID.y, z), tree02);
                if (p < 0.45f)
                {
                    type = BlockType.GetPlantBlockTypes();
                  

                 //  type = BlockType.Tree1;
                   

                }
          else if (p >= 0.45f&& p < 0.48f|| p >= 0.5 && p < 0.51f)
                    // else if (p >= 0.25f && p < 0.255f )
                        {
                   //    treePosDic2.Add(new Vector3Int(x,y-ID.y,z),tree01);

            
                   type = BlockType.Tree1;


                }


            }
        }

       
        return type;
    }
  public static  Dictionary<Vector3Int, BlockType> treePosDic = new Dictionary<Vector3Int, BlockType>();
    public bool Edge;
    void setBlock(Dictionary<Vector3Int,BlockType> treePosDic,Vector3Int treeRoot)
    {
        foreach (var kv in treePosDic)
        {
            var npos = kv.Key + treeRoot;

          // var chunk = ChunkManager.Instance.GetNearestChunk(npos);

         if (LocalPositionIsInRange(npos))
              //  if (npos.y<9)
                {
                //   _blocks.TryGetValue(npos,out ntype);
                 Blocks[npos.x, npos.y, npos.z]= kv.Value;
            }


            //  block = ChunkManager.Instance.GetBlockAtPosition(pos, ref chunk);

        }
    }

    public void BuildOpaqueMesh()
    {


   
        meshesIndex = 0;


   

        coubinMD = MeshData_.GetMax();


        float f = Time.realtimeSinceStartup;
      
        int ccc = 0;
        //  /*
        for (int x = 0; x < _size.x; x++)
        {


            for (int z = 0; z < _size.z; z++)

            {
            
           int maxy = hightmap[x, z];
             int miniy = GetMiny(x, z, maxy);

       for (int y = miniy; y < _size.y; y++)
                {
                  
                    BlockType block = Blocks[x, y, z];

                    if (block.isTransparent)

                        continue;

                  

                    indexOfExternalBlockArray++;
                    bool quauCount = GetVisibility002(x, y, z, visibility);
               
                    if (quauCount)
                    {
                     
                        ccc++;
                        int index = meshesIndex;
                      
                        int vc = block.GenerateMesh(visibility, coubinMD, ref meshesIndex);
                        for (int ii = 0; ii < vc; ii++)
                        {
                            int newIndex = index + ii;
                            Vector3 vp = coubinMD.vertexDate[newIndex].vertice;
                            vp.Set(vp.x + x, vp.y + y, vp.z + z);
                            coubinMD.vertexDate[newIndex].vertice = vp;

                        }

                    }
                    //  }



                }
            }
        }


        havegrass = true;

     
        if (meshesIndex == 0)
        {


                OpaqueMeshRenderer.renderingLayerMask = 0;
            MeshData_.ReturnMax(coubinMD);
            indexOfExternalBlockArray = 0;
            return;
        }
        if (!IsLoaded)
            IsLoaded = true;



        Mesh mesh = OpaqueMeshFilter.sharedMesh;
        MeshData_.Combine(this,  mesh);

        OpaqueMeshCol.sharedMesh = OpaqueMeshFilter.sharedMesh;
      
        ChunkManager.Instance.totolVc += OpaqueMeshFilter.sharedMesh.vertexCount;
        MeshData_.ReturnMax(coubinMD);
    
            if (LOD==0 )
            {
            OpaqueMeshRenderer.shadowCastingMode = ShadowCastingMode.On;

            OpaqueMeshRenderer.receiveShadows = true;

            OpaqueMeshCol.enabled = true;
            OpaqueMeshRenderer.sharedMaterial = ChunkManager.Instance.Opaque;
        }
        else
        {
            OpaqueMeshRenderer.sharedMaterial =ChunkManager.Instance. Opaquelod2;
        }
    
        if (OpaqueMeshRenderer.renderingLayerMask == 0)
            OpaqueMeshRenderer.renderingLayerMask = 1;
        return;
    }



    public void BuildWaterMesh()
    {
        meshesIndex = 0;
        coubinMD = MeshData_.GetMax();

        float f = Time.realtimeSinceStartup;

        int ccc = 0;
        //  /*
        for (int x = 0; x < _size.x; x++)
        {

            for (int z = 0; z < _size.z; z++)
            {
           
              for (int y = _sizeSmallOne.y; y > -1; y--)
        
                {

                    BlockType block = Blocks[x, y, z];
                    if (!block.isWater)
                        continue;

               
                    indexOfExternalBlockArray++;
                    bool quauCount = GetVisibilityWater(x, y, z, visibility, block);
                    if (quauCount)
                    {

                        ccc++;
                        int index = meshesIndex;
                        int vc = block.GenerateWaterFaces(visibility, coubinMD, ref meshesIndex);
                        for (int ii = 0; ii < vc; ii++)
                        {
                            int newIndex = index + ii;
                            Vector3 vp = coubinMD.vertexDate[newIndex].vertice;
                            vp.Set(vp.x + x, vp.y + y, vp.z + z);
                            coubinMD.vertexDate[newIndex].vertice = vp;

                       
                        }

                    }
                   break;
                    //  }



                }
            }
        }

        f = Time.realtimeSinceStartup - f;

        if (meshesIndex == 0)
        {
          //  if (WaterMeshRenderer.renderingLayerMask == 1)
                WaterMeshRenderer.renderingLayerMask = 0;
            MeshData_.ReturnMax(coubinMD);
            indexOfExternalBlockArray = 0;
            return;
        }
        if (!IsLoaded)
            IsLoaded = true;


        Mesh mesh = WaterMeshFilter.sharedMesh;
        MeshData_.Combine(this,  mesh);

        WaterMeshCol.sharedMesh = mesh;
 
        ChunkManager.Instance.totolVc += WaterMeshFilter.sharedMesh.vertexCount;
        MeshData_.ReturnMax(coubinMD);
   
        if (LOD == 0)
        {
            WaterMeshRenderer.shadowCastingMode = ShadowCastingMode.On;
           WaterMeshRenderer.receiveShadows = true;
            WaterMeshRenderer.sharedMaterial = ChunkManager.Instance.Water;
            WaterMeshCol.enabled = true;
        }
        else
        {
            WaterMeshRenderer.sharedMaterial = ChunkManager.Instance.WaterLod2;
        }
        if (WaterMeshRenderer.renderingLayerMask == 0)
            WaterMeshRenderer.renderingLayerMask = 1;
        return;
    }
    public bool sleep;
    public int LOD=-1;
    public float distance = -1;
    public List<Transform> treeslist2 = new List<Transform>();
    public void BuildFoliageMesh()
    {
        meshesIndex = 0;
        coubinMD = MeshData_.GetMax();

        float f = Time.realtimeSinceStartup;

        int ccc = 0;
        int treeCount=0;
        //  /*
        for (int x = 0; x < _size.x; x++)
        {

            for (int z = 0; z < _size.z; z++)
            {
           
               for (int y = _sizeSmallOne.y; y >-1; y--)
                  //  for (int y = 0; y < _size.y; y++)
             
                {

                    BlockType block = Blocks[x, y, z];
                    if (block == BlockType.Tree1 && !haveTree)
                    {
                        int treeindex0 = Random.Range(0, ChunkManager.Instance.trees.Count );
                        treeslistINDEX.Add(treeindex0);
                        treeslist2.Add(ObjPool.GetComponent<Transform>(ChunkManager.Instance.trees[treeindex0], new Vector3(ID.x + x, ID.y + y - 1.2f, ID.z + z), Quaternion.identity));
                        break;
                        treeCount++;
                  

                        break;
                    }

                    if (!block.isBillboard)
                        continue;
             
                    indexOfExternalBlockArray++;
                
                        ccc++;
                        int index = meshesIndex;
                        int vc = block.GenerateBillboardFaces( coubinMD, ref meshesIndex);
                        for (int ii = 0; ii < vc; ii++)
                        {
                            int newIndex = index + ii;
                            Vector3 vp = coubinMD.vertexDate[newIndex].vertice;
                            vp.Set(vp.x + x, vp.y + y, vp.z + z);
                            coubinMD.vertexDate[newIndex].vertice = vp;

                        
                        }

                    //暂时地形从上到下只有一个植物
                    break;
                    //  }



                }
            }
        }

        haveTree = true;

        if (meshesIndex == 0)
        {
          //  if (FoliageRenderer.renderingLayerMask == 1)
                FoliageMeshRenderer.renderingLayerMask = 0;
            MeshData_.ReturnMax(coubinMD);
            indexOfExternalBlockArray = 0;
            return;
        }
        if (!IsLoaded)
            IsLoaded = true;


        Mesh mesh = FoliageMeshFilter.sharedMesh;
        MeshData_.Combine(this,  mesh);

        FoliageMeshCol.sharedMesh = mesh;
    
        ChunkManager.Instance.totolVc += FoliageMeshFilter.sharedMesh.vertexCount;
        MeshData_.ReturnMax(coubinMD);
    
        if (LOD == 0)
        {
            FoliageMeshRenderer.shadowCastingMode = ShadowCastingMode.On;
           FoliageMeshRenderer.receiveShadows = true;

            FoliageMeshCol.enabled = true;
            FoliageMeshRenderer.sharedMaterial = ChunkManager.Instance.Foliage;
        }
        else
        {
            FoliageMeshRenderer.sharedMaterial = ChunkManager.Instance.Foliagelod2;
        }
        if (FoliageMeshRenderer.renderingLayerMask == 0)
            FoliageMeshRenderer.renderingLayerMask = 1;
        return;
    }
    /*
     
         */


    protected int GetMiny(int x, int z, int maxy)
    {


        int miny = maxy;
        int temp;

        if (x == 0 || z == 0 || x == _sizeSmallOne.x || z == _sizeSmallOne.z)
        {
            bool x0 = true; bool z0 = true;
            bool x1 = true; bool z1 = true;

            if (x == 0)
            {
                x1 = false;
                temp = NeighborLeft.hightmap[_sizeSmallOne.x, z];
                if (miny > temp)
                    miny = temp;

            }
            if (x == _sizeSmallOne.x)
            {
                x0 = false;
                temp = NeighborRight.hightmap[0, z];
                if (miny > temp)
                    miny = temp;
            }

            if (z == 0)
            {
                z1 = false;
                temp = NeighborBack.hightmap[x, _sizeSmallOne.z];
                if (miny > temp)
                    miny = temp;
            }
            if (z == _sizeSmallOne.z)
            {
                z0 = false;
                temp = NeighborForward.hightmap[x, 0];
                if (miny > temp)
                    miny = temp;
            }


            if (x0)
            {
                temp = hightmap[x + 1, z];
                if (miny > temp)
                    miny = temp;
            }
            if (x1)
            {
                temp = hightmap[x - 1, z];
                if (miny > temp)
                    miny = temp;
            }
            if (z0)
            {
                temp = hightmap[x, z + 1];
                if (miny > temp)
                    miny = temp;
            }
            if (z1)
            {
                temp = hightmap[x, z - 1];
                if (miny > temp)
                    miny = temp;

            }

            return miny;
        }
        else
        {
            temp = hightmap[x + 1, z];
            if (miny > temp)
                miny = temp;
            temp = hightmap[x - 1, z];
            if (miny > temp)
                miny = temp;
            temp = hightmap[x, z + 1];
            if (miny > temp)
                miny = temp;
            temp = hightmap[x, z - 1];
            if (miny > temp)
                miny = temp;
            return miny;
        }








    }


    protected bool GetVisibility002(int x, int y, int z, bool[] visibility_)
    {

        for (int i = 0; i < visibility.Length; i++)
        {
            if (visibility_[i])

                visibility_[i] = false;

        }

        // Up, Down, Front, Back, Left, Right


        //chunk 外围的方块
        // if(block.side)
        if (x == 0 || y == 0 || z == 0 || x == _sizeSmallOne.x || y == _sizeSmallOne.y || z == _sizeSmallOne.z)
        {


            if (y == _sizeSmallOne.y)
            {
                if (!NeighborUp.isEmperty)
                {
                    visibility_[0] = NeighborUp.Blocks[x, 0, z].isTransparent;
                    neighborsBlock[0] = true;

                }
                else
                {
                    visibility_[0] = true;
                    neighborsBlock[0] = true;
                }

            }
            ////////////////////
            if (y == 0)
            {


                if (NeighborDown)
                {


                    if (!NeighborDown.isEmperty)
                        visibility_[1] = NeighborDown.Blocks[x, _sizeSmallOne.y, z].isTransparent;
                    else
                        visibility_[1] = false;
                    neighborsBlock[1] = true;




                }
                else
                {
                    // ChunkManager.Instance.c4++;
                    //  Vector3Int wp = new Vector3Int(x, y, z);
                    // wp = this.LocalToWorldPosition(wp);
                    //visibility_[1] = WorldChunk.GetBlockType(wp.x, wp.y, wp.z, _seed).isTransparent;
                    // neighborsBlock[1] = true;

                    visibility_[1] = false;
                    neighborsBlock[1] = true;
                }


            }

            ////////////////////
            if (x == _sizeSmallOne.x)
            {


                if (NeighborRight.computedTerrainDate)
                    visibility_[2] = NeighborRight.Blocks[0, y, z].isTransparent;
              
                else
                    visibility_[2] = true;
                neighborsBlock[2] = true;


            }

            if (x == 0)
            {



                if (NeighborLeft.computedTerrainDate)
                    visibility_[3] = NeighborLeft.Blocks[_sizeSmallOne.x, y, z].isTransparent;
             
                else
                    visibility_[3] = true;
                neighborsBlock[3] = true;

            }

            if (z == 0)
            {


                if (NeighborBack.computedTerrainDate)
                    visibility_[5] = NeighborBack.Blocks[x, y, _sizeSmallOne.z].isTransparent;
              
                else
                    visibility_[5] = true;
                neighborsBlock[5] = true;



            }
            if (z == _sizeSmallOne.z)
            {



                if (NeighborForward.computedTerrainDate)
                    visibility_[4] = NeighborForward.Blocks[x, y, 0].isTransparent;
          
                else
                    visibility_[4] = true;
                neighborsBlock[4] = true;

            }




            if (neighborsBlock[0])
                neighborsBlock[0] = false;
            else
                visibility_[0] = Blocks[x, y + 1, z].isTransparent;

            if (neighborsBlock[1])
                neighborsBlock[1] = false;
            else
                visibility_[1] = Blocks[x, y - 1, z].isTransparent;

            if (neighborsBlock[2])
                neighborsBlock[2] = false;
            else
                visibility_[2] = Blocks[x + 1, y, z].isTransparent;

            if (neighborsBlock[3])
                neighborsBlock[3] = false;
            else
                visibility_[3] = Blocks[x - 1, y, z].isTransparent;

            if (neighborsBlock[4])
                neighborsBlock[4] = false;
            else
                visibility_[4] = Blocks[x, y, z + 1].isTransparent;

            if (neighborsBlock[5])
                neighborsBlock[5] = false;
            else
                visibility_[5] = Blocks[x, y, z - 1].isTransparent;







            for (int ni = 0; ni < 6; ni++)
            {
                if (visibility_[ni])
                    return true;
            }

            return false;
        }

        visibility_[0] = Blocks[x, y + 1, z].isTransparent;

        visibility_[1] = Blocks[x, y - 1, z].isTransparent;

        visibility_[2] = Blocks[x + 1, y, z].isTransparent;

        visibility_[3] = Blocks[x - 1, y, z].isTransparent;

        visibility_[4] = Blocks[x, y, z + 1].isTransparent;

        visibility_[5] = Blocks[x, y, z - 1].isTransparent;

        for (int ni = 0; ni < 6; ni++)
        {
            if (visibility_[ni])
                return true;
        }

        return false;

    }

    protected bool GetVisibilityWater(int x, int y, int z, bool[] visibility_, BlockType block)
    {
   
        if (y == _sizeSmallOne.y)
        {
            if (NeighborUp.computedTerrainDate)
            {
                var temp2 = NeighborUp.Blocks[x, 0, z];
                if (temp2.isWater)

                    return false;
                else
               {
                    return true;
                }

             

            }
            else
            {
                return true;
             
            }

        }


        var temp = Blocks[x, y + 1, z];
        if (temp.isWater)

            return false;
        else
        {
            return true;
        }

       

    }



    public void loadNeighborsBlocks()
    {
        if (!computedTerrainDate)
            CreateTerrainDate();

        if (!NeighborRight.computedTerrainDate)
            NeighborRight.CreateTerrainDate();

        if (!NeighborLeft.computedTerrainDate)
            NeighborLeft.CreateTerrainDate();

        if (!NeighborForward.computedTerrainDate)
            NeighborForward.CreateTerrainDate();

        if (!NeighborBack.computedTerrainDate)
            NeighborBack.CreateTerrainDate();

        if (!NeighborUp.computedTerrainDate)
            NeighborUp.CreateTerrainDate();


        //  if (NeighborDown)
        //   {
        if (!NeighborDown.computedTerrainDate)
            NeighborDown.CreateTerrainDate();
        //    }


        //  block.side = false;
        // BlockType ntype;





        return;

    }

  
  
   
    public Vector3Int WorldToLocalPosition(Vector3Int worldPos)
    {
        return worldPos - ID;
    }

    private bool LocalPositionIsInRange(Vector3Int localPos)
    {
        //  return localPos.x >= 0 && localPos.z >= 0 && localPos.x < _size.x && localPos.z < _size.z && localPos.y >= 0 && localPos.y < _size.y;
        if (localPos.x > _sizeSmallOne.x || localPos.y > _sizeSmallOne.y || localPos.z > _sizeSmallOne.z || localPos.x < 0 || localPos.y < 0 || localPos.z < 0)
        {
            return false;
        }
        else
        {
            return true;
        }
    }

    //chunk坐标
    public BlockType GetBlockAtWorldPosition(Vector3Int worldPos)
    {


        if (isAir)
        {
            return BlockType.Air;
        }

        Vector3Int localPos = WorldToLocalPosition(worldPos);
        return GetBlockAtChunkPos(localPos.x, localPos.y, localPos.z);
    }


    public BlockType GetBlockAtChunkPos(int x, int y, int z)
    {



        if (!computedTerrainDate)
        {
            CreateTerrainDate();
        }
        return Blocks[x, y, z];
    }


    public void DesttroyBlocksFun()
    {


        foreach (var lpos in DestroyBlocks)
        {
            BlockType block = Blocks[lpos.x, lpos.y, lpos.z];
            if (block.isTree)
            {
                Blocks[lpos.x, lpos.y, lpos.z]=BlockType.Air;
                continue;
            }
            if (block.isPlant)
            {

            }
            bool haveWaterAround = false;//周围有没有水

            int x = lpos.x; int y = lpos.y; int z = lpos.z;



            if (lpos.y == _sizeSmallOne.y)
            {
                if (!haveWaterAround)
                {
                    if (NeighborUp.GetBlockAtChunkPos(lpos.x, 0, lpos.z).isWater)
                        haveWaterAround = true;
                }
                // hightmap[lpos.x, lpos.z]--;
                if (!NeighborUp.isEmperty)
                {

                    ChunkManager.TouchedChunks.Add(NeighborUp);
                    NeighborUp.TouchedBlocks.Add(new Vector3Int(lpos.x, 0, lpos.z));

                }


            }
            if (lpos.y == 0)
            {


              if (!NeighborDown)
                {


                    var wp = new Vector3Int(ID.x, ID.y - ChunkManager.Instance.chunkSize.y, ID.z);
                    if (!ChunkManager._chunks.TryGetValue(wp, out NeighborDown))
                    {
                        NeighborDown = GetChunk();
                        NeighborDown.Initialize(wp);
                        NeighborDown.InitializeNeighbors();

                        NeighborDown.CreateTerrainDate();

                    }
                    NeighborDown.NeighborUp = this;





                }
                ChunkManager.TouchedChunks.Add(NeighborDown);
                NeighborDown.TouchedBlocks.Add(new Vector3Int(lpos.x, _sizeSmallOne.y, lpos.z));


            }

            if (lpos.x == 0)
            {
                if (!haveWaterAround)
                {
                    if (NeighborLeft.GetBlockAtChunkPos(_sizeSmallOne.x, lpos.y, lpos.z).isWater)
                        haveWaterAround = true;
                }
                ChunkManager.TouchedChunks.Add(NeighborLeft);
                NeighborLeft.TouchedBlocks.Add(new Vector3Int(_sizeSmallOne.x, lpos.y, lpos.z));
                if (Blocks[lpos.x + 1, lpos.y, lpos.z].isWater)
                {
                    haveWaterAround = true;
                }
            }
            else if (lpos.x == _sizeSmallOne.x)
            {

                if (!haveWaterAround)
                {
                    if (NeighborRight.GetBlockAtChunkPos(0, lpos.y, lpos.z).isWater)
                        haveWaterAround = true;
                }
                ChunkManager.TouchedChunks.Add(NeighborRight);
                NeighborRight.TouchedBlocks.Add(new Vector3Int(0, lpos.y, lpos.z));
                if (Blocks[lpos.x - 1, lpos.y, lpos.z].isWater)
                {
                    haveWaterAround = true;
                }

            }
            else
            {
                if (Blocks[lpos.x + 1, lpos.y, lpos.z].isWater)
                {
                    haveWaterAround = true;
                }
                else if (Blocks[lpos.x - 1, lpos.y, lpos.z].isWater)
                {
                    haveWaterAround = true;
                }
            }


            if (lpos.z == 0)
            {

                if (!haveWaterAround)
                {
                    if (NeighborBack.GetBlockAtChunkPos(lpos.x, lpos.y, _sizeSmallOne.z).isWater)
                        haveWaterAround = true;
                }

                ChunkManager.TouchedChunks.Add(NeighborBack);
                NeighborBack.TouchedBlocks.Add(new Vector3Int(lpos.x, lpos.y, _sizeSmallOne.z));

                if (Blocks[lpos.x, lpos.y, lpos.z + 1].isWater)
                {
                    haveWaterAround = true;
                }

            }
            else if (lpos.z == _sizeSmallOne.z)
            {
                if (!haveWaterAround)
                {
                    if (NeighborForward.GetBlockAtChunkPos(lpos.x, lpos.y, 0).isWater)
                        haveWaterAround = true;
                }
                ChunkManager.TouchedChunks.Add(NeighborForward);
                NeighborForward.TouchedBlocks.Add(new Vector3Int(lpos.x, lpos.y, 0));

                if (Blocks[lpos.x, lpos.y, lpos.z - 1].isWater)
                {
                    haveWaterAround = true;
                }

            }
            else
            {
                if (Blocks[lpos.x, lpos.y, lpos.z + 1].isWater)
                {
                    haveWaterAround = true;
                }
                if (Blocks[lpos.x, lpos.y, lpos.z - 1].isWater)
                {
                    haveWaterAround = true;
                }
            }





            if (block.isBillboard)
            {
                IsRebuildFoliageMesh = true;
            }
            if (block.isWater)
            {
                IsRebuildWaterMesh = true;
            }
          //  Debug.Log(block.blockName+"  "+ block.isTransparent);
            if (!block.isTransparent)
            {
                IsRebuildOpaqueMesh = true;
                // if (lpos.y != 0)
                int h = hightmap[lpos.x, lpos.z];
          //   Debug.Log(lpos+" zzzzzz "+h );
                if (h>= lpos.y  )
                {
                   
                        h = lpos.y - 1;
                    if (h == -1)
                        h = 0;
                    hightmap[lpos.x, lpos.z] = h;




                 //   Debug.Log( hightmap[lpos.x, lpos.z]);
                }


            }
         

            if (haveWaterAround)
            {
                Blocks[lpos.x, lpos.y, lpos.z] = BlockType.Water;
                IsRebuildWaterMesh = true;
                //还要考虑四周是不是有空方块
                WaterFlowCheck(lpos.x, lpos.y, lpos.z);
            }
            else
            {
                Blocks[lpos.x, lpos.y, lpos.z] = BlockType.Air;
                if (block.blockName == BlockNameEnum.Grass)
                {
                    if (lpos.y<_sizeSmallOne.y)
                    {
                        if (Blocks[lpos.x, lpos.y+1, lpos.z].isPlant)
                        {
                            Blocks[lpos.x, lpos.y+1, lpos.z] = BlockType.Air;
                        }
                    }
                    else
                    {
                        if (NeighborUp. Blocks[lpos.x, 0, lpos.z].isPlant)
                        {
                            NeighborUp.Blocks[lpos.x,0, lpos.z] = BlockType.Air;
                        }
                    }
                }
            }








        }

        ChunkManager.TouchedChunks.Add(this);

        DestroyBlocks.Clear();

        return;
    }



    public void WaterFlowCheck(int x, int y, int z)
    {
        //可以通过设置气压来模拟喷泉
        //  if (y == _sizeSmallOne.y)
        //  {


        //   }

        if (y == 0)
        {
            if (NeighborDown.Blocks[x, _sizeSmallOne.y, z].isAir)
            {
                NeighborDown.Blocks[x, _sizeSmallOne.y, z] = BlockType.Water;
                NeighborDown.WaterFlowCheck(x, _sizeSmallOne.y, z);
                //  GameObject.Instantiate(chunkManager.ggg, NeighborDown. LocalToWorldPosition(new Vector3Int(x, _sizeSmallOne.y, z)), Quaternion.identity);
            }


            ChunkManager.TouchedChunks.Add(NeighborDown);
            NeighborDown.TouchedBlocks.Add(new Vector3Int(x, _sizeSmallOne.y, z));

        }
        else
        {

            if (Blocks[x, y - 1, z].isAir)
            {
                Blocks[x, y - 1, z] = BlockType.Water;
                //     GameObject.Instantiate(chunkManager.ggg,LocalToWorldPosition(new Vector3Int(x,y-1,z)),Quaternion.identity);
                WaterFlowCheck(x, y - 1, z);
            }
        }

        if (x == 0)
        {
            if (NeighborLeft.Blocks[_sizeSmallOne.x, y, z].isAir)
            {
                NeighborLeft.Blocks[_sizeSmallOne.x, y, z] = BlockType.Water;
                NeighborLeft.WaterFlowCheck(_sizeSmallOne.x, y, z);
            }

            ChunkManager.TouchedChunks.Add(NeighborLeft);
            NeighborLeft.TouchedBlocks.Add(new Vector3Int(_sizeSmallOne.x, y, z));
        }
        else if (x == _sizeSmallOne.x)
        {
            if (NeighborRight.Blocks[0, y, z].isAir)
            {
                NeighborRight.Blocks[0, y, z] = BlockType.Water;
                NeighborRight.WaterFlowCheck(0, y, z);
            }




            ChunkManager.TouchedChunks.Add(NeighborRight);
            NeighborRight.TouchedBlocks.Add(new Vector3Int(0, y, z));
        }
        else
        {
            if (Blocks[x + 1, y, z].isAir)
            {
                Blocks[x + 1, y, z] = BlockType.Water;
                WaterFlowCheck(x + 1, y, z);
            }
            if (Blocks[x - 1, y, z].isAir)
            {
                Blocks[x - 1, y, z] = BlockType.Water;
                WaterFlowCheck(x - 1, y, z);
            }
        }


        if (z == 0)
        {
            if (NeighborBack.Blocks[x, y, _sizeSmallOne.z].isAir)
            {
                NeighborBack.Blocks[x, y, _sizeSmallOne.z] = BlockType.Water;
                NeighborBack.WaterFlowCheck(x, y, _sizeSmallOne.z);
            }


            ChunkManager.TouchedChunks.Add(NeighborBack);
            NeighborBack.TouchedBlocks.Add(new Vector3Int(x, y, _sizeSmallOne.z));

        }
        else if (z == _sizeSmallOne.z)
        {
            if (NeighborForward.Blocks[x, y, 0].isAir)
            {
                NeighborForward.Blocks[x, y, 0] = BlockType.Water;
                NeighborForward.WaterFlowCheck(x, y, 0);
            }


            ChunkManager.TouchedChunks.Add(NeighborForward);
            NeighborForward.TouchedBlocks.Add(new Vector3Int(x, y, 0));
        }
        else
        {
            if (Blocks[x, y, z + 1].isAir)
            {
                Blocks[x, y, z + 1] = BlockType.Water;
                WaterFlowCheck(x, y, z + 1);
            }
            if (Blocks[x, y, z - 1].isAir)
            {
                Blocks[x, y, z - 1] = BlockType.Water;
                WaterFlowCheck(x, y, z - 1);
            }
        }



    }

    public bool removeFlag;

 

    public static Stack<WorldChunk> pool = new Stack<WorldChunk>(10000);
    public static WorldChunk GetChunk()
    {
        WorldChunk chunk;
        if (pool.Count > 0)
        {
            chunk = pool.Pop();
        }
        else
        {
            chunk = Instantiate<WorldChunk>(ChunkManager.Instance.ChunkPrefab);
        }
        return chunk;
    }



}
