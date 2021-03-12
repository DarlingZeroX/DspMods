using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections.Generic;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;

namespace SelectBuild
{
    class SelectedFrame
    {
        private bool __enable = false;
        public bool enable{
            get
            {
                return __enable;
            }
            private  set
            {
                __enable = value;
            }
        }

        private float anchorScreenX;
        private float anchorScreenY;
        private Vector3 anchorPos;
        private Vector3 mousePos;
        private float minX, minY, maxX, maxY;

        public void Update()
        {
            mousePos = Input.mousePosition;
            //Min And Max X
            if (mousePos.x > anchorPos.x)
            {
                minX = anchorPos.x;
                maxX = mousePos.x;
            }
            else
            {
                minX = mousePos.x;
                maxX = anchorPos.x;
            }

            //Min And Max Y
            if (mousePos.y > anchorPos.y)
            {
                minY = anchorPos.y;
                maxY = mousePos.y;
            }
            else
            {
                minY = mousePos.y;
                maxY = anchorPos.y;
            }
        }

        private float GetScreenY(float y)
        {
            return 1 - (y / Screen.height);
        }

        private float GetScreenX(float x)
        {
            return x / Screen.width;
        }

        public void DrawDestructFrame()
        {
            Vector3 _mousePos = Input.mousePosition;

            float xScale = GetScreenX(_mousePos.x) - anchorScreenX;
            float yScale = GetScreenY(_mousePos.y) - anchorScreenY;

            GIRer.DrawFrame2D(anchorScreenX, anchorScreenY, xScale, yScale, 0.5f, 0.0f, 0.0f, 1.0f);
            GIRer.DrawRect2D(anchorScreenX, anchorScreenY, xScale, yScale, 1.0f, 0.0f, 0.0f, 0.1f);
        }

        public void DrawUpgradeFrame()
        {
            Vector3 _mousePos = Input.mousePosition;

            float xScale = GetScreenX(_mousePos.x) - anchorScreenX;
            float yScale = GetScreenY(_mousePos.y) - anchorScreenY;

            GIRer.DrawFrame2D(anchorScreenX, anchorScreenY, xScale, yScale, 0.0f, 0.5f, 0.0f, 1.0f);
            GIRer.DrawRect2D(anchorScreenX, anchorScreenY, xScale, yScale, 0.0f, 1.0f, 0.0f, 0.1f);
        }

        public void Enable(Vector3 _anchorPos)
        {
            enable = true;

            anchorPos = _anchorPos;
            anchorScreenX = GetScreenX(_anchorPos.x);
            anchorScreenY = GetScreenY(_anchorPos.y);
        }

        public void Disable()
        {
            enable = false;
        }
        /*
        public Vector3 GetWorldCenter(Vector3 clickPoint)
        {
            Vector3 res = new Vector3();
            res.x = (clickPoint.x - anchorPos.x) * 0.5f + anchorPos.x;
            res.y = (anchorPos.y - clickPoint.y) * 0.5f + clickPoint.y;
            res.z = 0.0f;
            return res;
        }
        public Vector3 GetWorldSize(Vector3 clickPoint)
        {
            Vector3 size = Vector3.one;
            Vector3 rtPoint = Vector3.one;
            rtPoint.x = clickPoint.x;
            rtPoint.y = anchorPos.y;
            rtPoint.z = anchorPos.z = clickPoint.z = Camera.main.farClipPlane;
            rtPoint = Camera.main.ScreenToWorldPoint(rtPoint);


            size.x = (rtPoint - Camera.main.ScreenToWorldPoint(anchorPos)).magnitude;
            size.y = (Camera.main.ScreenToWorldPoint(clickPoint) - rtPoint).magnitude;

            return size;
        }
        public Vector3 GetDir()
        {
            Ray myRay = Camera.main.ScreenPointToRay(Input.mousePosition);
            return myRay.direction;
        }*/

        public bool inside(Vector3 point)
        {
            //compare
            if(point.x >= minX &&
               point.x <= maxX &&
               point.y >= minY &&
               point.y <= maxY)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public void HandlePlayerInput(PlayerAction_Build __instance)
        {
            if (Input.GetKeyDown(KeyCode.LeftControl))
            {
                Enable(Input.mousePosition);
            }
            else if (Input.GetKeyUp(KeyCode.LeftControl))
            {
                Disable();
            }
        }

    }

    [BepInPlugin(__GUID__, __NAME__, "1.1")]
    public class SelectBuild : BaseUnityPlugin
    {
        public const string __NAME__ = "SelectBuild";
        public const string __GUID__ = "0x.plugins.dsp." + __NAME__;

        static Player player = null;
        static RaycastLogic raycastLogic = null;
        static SelectedFrame selectedFrame;
        static VectorLF3 playerScale = VectorLF3.one;
        public static bool GIRerInit = false;

        void Start()
        {
            GIRer.Enable();
            selectedFrame = new SelectedFrame();
            new Harmony(__GUID__).PatchAll();
        }

        void Update()
        {
            if(player != null)
            {
                if(Input.GetKeyDown(KeyCode.F1))
                {
                    playerScale =  playerScale * 2.0f;
                    player.gameObject.transform.localScale = playerScale;
                }
                else if (Input.GetKeyDown(KeyCode.F2))
                {
                    playerScale = playerScale / 2.0f;
                    player.gameObject.transform.localScale = playerScale;
                }
                else if (Input.GetKeyDown(KeyCode.F3))
                {
                    GameObject PlayerObject = Instantiate(player.gameObject);
                }
            }

            if(GIRerInit)
                GIRer.Update();
        }

        static unsafe void DrawFrame()
        {
            if (selectedFrame.enable)
            {
                Debug.Log("----------RenderCallback----------");
                switch (player.controller.cmd.mode)
                {
                    case -1:
                        selectedFrame.DrawDestructFrame();
                        break;
                    case -2:
                        selectedFrame.DrawUpgradeFrame();
                        break;
                }
            }
        }

        [HarmonyPatch(typeof(PlayerController), "Init")]
        public class PlayerControllerInit
        {

            public static void Postfix(PlayerController __instance)
            {
                player = __instance.player;

                if (GIRerInit == false)
                {
                    GIRer.AddRenderCallback(DrawFrame);
                    GIRerInit = true;
                }
            }
        }

        class Building
        {
            public delegate void BuildingFilter(ColliderData colliderData);

            /// <summary>
            /// Extract from RaycastLogic::GameTick
            /// Use to filter buidling
            /// </summary>
            public static void Select(RaycastLogic __instance, BuildingFilter filter)
            {
                int activeColHashCount = Traverse.Create((object)__instance).Field("activeColHashCount").GetValue<int>();
                int[] activeColHashes = Traverse.Create((object)__instance).Field("activeColHashes").GetValue<int[]>();
                ColliderContainer[] colChunks = Traverse.Create((object)__instance).Field("colChunks").GetValue<ColliderContainer[]>();

                if (!__instance.doCast || GameMain.localPlanet != __instance.planet || (VFInput.inFullscreenGUI || !Application.isFocused))
                    return;

                for (int i = 0; i < activeColHashCount; i++)
                {
                    int activeColHash = activeColHashes[i];
                    ColliderData[] colliderPool = colChunks[activeColHash].colliderPool;

                    for (int j = 1; j < colChunks[activeColHash].cursor; j++)
                    {
                        if (colliderPool[j].idType != 0)
                        {
                            EObjectType objType = colliderPool[j].objType;
                            bool typeFlag = objType == EObjectType.Vein || objType == EObjectType.Vegetable;
                            if (!typeFlag || !__instance.ignoreVegeAndVein)
                            {
                                if ((colliderPool[j].usage == EColliderUsage.Build || typeFlag)
                                    && colliderPool[j].shape == EColliderShape.Box)
                                {
                                    //Send to filter
                                    filter(colliderPool[j]);
                                }
                            }
                        }
                    }

                }

            }
        }
     
        public class DeterminePreviews
        {
            protected static List<int> entityIdList = new List<int>();
            protected static List<EObjectType> entityTypeList = new List<EObjectType>();
            private static int selectedIndex = -1;

            public delegate bool PreviewsFilter(ItemProto itemProto);

            /// <summary>
            /// Filter building type during construction preview
            /// </summary>
            public static bool PreviewsTypeFilter(ItemProto itemProto)
            {
                if (selectedIndex != -1 && selectedIndex != itemProto.BuildIndex)
                    return true;
                return false;
            }

            /// <summary>
            /// Filter building whether inside frame
            /// </summary>
            public static void FrameInsideFilter(ColliderData colliderData)
            {
                if (selectedFrame.inside(Camera.main.WorldToScreenPoint(colliderData.pos)))
                {     
                    //Extract from 
                    int entityId = colliderData.objType != EObjectType.Entity ? -colliderData.objId : colliderData.objId;

                    entityIdList.Add(entityId);         //entityIdList.Add(colliderData.objId);
                    entityTypeList.Add(colliderData.objType);
                }
            }

            /// <summary>
            /// Handle player appoint building type
            /// </summary>
            public static void HandlePlayerAppoint(PlayerAction_Build __instance)
            {
                if (Input.GetKeyDown(KeyCode.LeftAlt))
                {
                    if (__instance.castObjId != 0)
                    {
                        ItemProto itemProto = (ItemProto)Traverse.Create(__instance).Method("GetItemProto", __instance.castObjId).GetValue();
                        selectedIndex = itemProto.BuildIndex;
#if DEBUG
                        ModDebug.Log("Select Building Type:" + selectedIndex);
#endif
                    }
                    else
                    {
                        selectedIndex = -1;
#if DEBUG
                        ModDebug.Log("Select All Building");
#endif
                    }
                }
            }

            /// <summary>
            /// Preprocess building preview
            /// </summary>
            /// <returns>If preview need process return false</returns>
            public static bool PrefixPreProcess(PlayerAction_Build __instance)
            {
                //Get mouse raycast building type
                HandlePlayerAppoint(__instance);

                //Update SelectedFrame
                selectedFrame.HandlePlayerInput(__instance);
                selectedFrame.Update();

                //Enable select frame
                if (selectedFrame.enable == false)
                    return true;

                //Get Select Building
                ClearEntityList();
                Building.Select(raycastLogic, FrameInsideFilter);
#if DEBUG
                //Debug.Log("Select Count:" + entityIdList.Count);
#endif
                return false;
            }

            public static void ClearEntityList()
            {
                entityIdList.Clear();
                entityTypeList.Clear();
            }
        }

        [HarmonyPatch(typeof(PlayerAction_Build), "DetermineDestructPreviews")]
        public class DetermineDestructPreviews: DeterminePreviews
        {
            /// <summary>
            /// Orignal PlayerAction_Build::DetermineDestructPreviews
            /// </summary>
            public static void CreateEntityDestructPreview(
                PlayerAction_Build __instance, 
                List<int> idList, 
                List<EObjectType> typeList,
                DeterminePreviews.PreviewsFilter filter
                )
            {
                if (!VFInput.onGUI)
                    UICursor.SetCursor(ECursor.Delete);

                __instance.previewPose.position = Vector3.zero;
                __instance.previewPose.rotation = Quaternion.identity;

                __instance.ClearBuildPreviews();

                foreach (int entityId in idList)
                {
                    //int entityId = typeList[index] != EObjectType.Entity ? -entityIdList[index] : entityIdList[index];

                    ItemProto itemProto = (ItemProto)Traverse.Create(__instance).Method("GetItemProto", entityId).GetValue();
                    Pose objectPose = (Pose)Traverse.Create(__instance).Method("GetObjectPose", entityId).GetValue();
       
                    if (itemProto != null && 
                        filter(itemProto) == false)
                    {
                        //Add Build Preview
                        __instance.AddBuildPreview(new BuildPreview());

                        BuildPreview buildPreview = __instance.buildPreviews[__instance.buildPreviews.Count - 1];
                        buildPreview.item = itemProto;
                        buildPreview.desc = itemProto.prefabDesc;
                        buildPreview.lpos = objectPose.position;
                        buildPreview.lrot = objectPose.rotation;
                        buildPreview.objId = entityId;

                        int num = buildPreview.desc.lodCount <= 0 ? 0 : ((Object)buildPreview.desc.lodMeshes[0] != (Object)null ? 1 : 0);
                        buildPreview.needModel = num != 0;
                        buildPreview.isConnNode = true;

                        if (buildPreview.desc.isInserter)
                        {
                            Pose objectPose2 = (Pose)Traverse.Create(__instance).Method("GetObjectPose2", buildPreview.objId).GetValue();
                            buildPreview.lpos2 = objectPose2.position;
                            buildPreview.lrot2 = objectPose2.rotation;
                        }

                        PlanetData planetData = __instance.player.planetData;
                        Vector3 vector3_1 = __instance.player.position;

                        if (planetData.type == EPlanetType.Gas)
                        {
                            vector3_1 = vector3_1.normalized;
                            Vector3 vector3_2 = vector3_1 * planetData.realRadius;
                        }
                        else
                        {
                            buildPreview.condition = EBuildCondition.Ok;
                            __instance.cursorText = "拆除".Translate() + buildPreview.item.name + "\r\n" + "连锁拆除提示".Translate();
                        }

                        if (buildPreview.desc.multiLevel)
                        {
                            int otherObjId;
                            PlanetFactory factory = Traverse.Create((object)__instance).Field("factory").GetValue<PlanetFactory>();
                            factory.ReadObjectConn(buildPreview.objId, 15, out bool _, out otherObjId, out int _);
                            if ((uint)otherObjId > 0U)
                            {
                                buildPreview.condition = EBuildCondition.Covered;
                                __instance.cursorText = buildPreview.conditionText;
                            }
                        }                  
                    }
                }
            }

            public static bool Prefix(PlayerAction_Build __instance)
            {
                if (PrefixPreProcess(__instance) == true)
                    return true;

                CreateEntityDestructPreview(__instance, entityIdList, entityTypeList, PreviewsTypeFilter);             
                return false;
            }          
        }

        [HarmonyPatch(typeof(PlayerAction_Build), "DetermineUpgradePreviews")]
        public class DetermineUpgradePreviews : DeterminePreviews
        {
            /// <summary>
            /// Orignal PlayerAction_Build::DetermineDestructPreviews
            /// </summary>
            public static void CreateEntityUpgradePreview(
                PlayerAction_Build __instance,
                List<int> idList,
                List<EObjectType> typeList,
                DeterminePreviews.PreviewsFilter filter
                )
            {
                if (!VFInput.onGUI)
                {
                    if (__instance.upgradeLevel == 1)
                        UICursor.SetCursor(ECursor.Upgrade);
                    else if (__instance.upgradeLevel == -1)
                        UICursor.SetCursor(ECursor.Downgrade);
                }

                __instance.previewPose.position = Vector3.zero;
                __instance.previewPose.rotation = Quaternion.identity;

                __instance.ClearBuildPreviews();
                foreach (int entityId in idList)
                {
                    ItemProto itemProto = (ItemProto)Traverse.Create(__instance).Method("GetItemProto", entityId).GetValue();
                    Pose objectPose = (Pose)Traverse.Create(__instance).Method("GetObjectPose", entityId).GetValue();

                    bool flag = false;
                    if (itemProto != null && itemProto.Grade > 0 && itemProto.Upgrades.Length > 0)
                        flag = true;

                    if (flag &&
                        filter(itemProto) == false)
                    {
                        __instance.AddBuildPreview(new BuildPreview());

                        BuildPreview buildPreview = __instance.buildPreviews[__instance.buildPreviews.Count - 1];
                        buildPreview.item = itemProto;
                        buildPreview.desc = itemProto.prefabDesc;
                        buildPreview.lpos = objectPose.position;
                        buildPreview.lrot = objectPose.rotation;
                        buildPreview.objId = entityId;

                        if (buildPreview.desc.lodCount > 0 && buildPreview.desc.lodMeshes[0] != null)
                            buildPreview.needModel = true;
                        else
                            buildPreview.needModel = false;
                        buildPreview.isConnNode = true;

                        if (buildPreview.desc.isInserter)
                        {
                            Pose objectPose2 = (Pose)Traverse.Create(__instance).Method("GetObjectPose2", buildPreview.objId).GetValue();
                            //Oringal pose objectPose2 = __instance.GetObjectPose2(buildPreview.objId);
                            buildPreview.lpos2 = objectPose2.position;
                            buildPreview.lrot2 = objectPose2.rotation;
                        }
                        if ((double)(buildPreview.lpos - __instance.player.position).sqrMagnitude > (double)__instance.player.mecha.buildArea * (double)__instance.player.mecha.buildArea)
                        {
                            buildPreview.condition = EBuildCondition.OutOfReach;
                            __instance.cursorText = "目标超出范围".Translate();
                            __instance.cursorWarning = true;
                        }
                        else
                        {
                            buildPreview.condition = EBuildCondition.Ok;
                            __instance.cursorText = "升级".Translate() + buildPreview.item.name + "\r\n" + "连锁升级提示".Translate();
                        }
                    }
                }
            }

            public static bool Prefix(PlayerAction_Build __instance)
            {
                if (PrefixPreProcess(__instance) == true)
                    return true;

                CreateEntityUpgradePreview(__instance, entityIdList, entityTypeList, PreviewsTypeFilter);              
                return false;
            }
        }

        [HarmonyPatch(typeof(RaycastLogic), "Init")]
        public class RaycastLogicClass
        {
            public static void Prefix(RaycastLogic __instance)
            {
                raycastLogic = __instance;
            }
        }

    }
}