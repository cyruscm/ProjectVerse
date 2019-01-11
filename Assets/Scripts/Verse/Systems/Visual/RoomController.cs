﻿using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse.API.Models;
using Verse.Utilities;

namespace Verse.Systems.Visual {
    public class RoomController : MonoBehaviour {
        public GameObject TerrainRoot;
        public GameObject ObjectRoot;
        public GameObject TerrainTilePrefab;
        public GameObject ObjectTilePrefab;
        public GameObject TransparencyColliderPrefab;

        private ObjectAtlas _objectAtlas;

        public Dictionary<GameObject, TileObject> activeObjects;
        public Dictionary<GameObject, Tile> activeTerrainTiles;

        public string CurrentRoom { get; private set; }

        public bool HasActiveRoom { get; private set; }

        public Position TopRight { get; private set; }
        public Position BottomLeft { get; private set; }
        public Position Center { get; private set; }

        public static RoomController Instance;

        private void Awake() {
            Instance = this;
        }

        private void Start() {
            activeObjects = new Dictionary<GameObject, TileObject>();
            activeTerrainTiles = new Dictionary<GameObject, Tile>();
            //BuildRoom("main");
        }

        public void ChangeRoom(string newRoom, string oldRoom) {
            DestroyRoom();
            BuildRoom(newRoom);
        }

        public ScriptableTileObject GetScriptableThingFromGameObject(GameObject go) {
            return (ScriptableTileObject) activeObjects[go];
        }

        public void DestroyRoom() {
            foreach (var go in activeObjects.Keys) {
                foreach (Transform child in go.transform) {
                    SimplePool.Despawn(child.gameObject);
                }

                SimplePool.Despawn(go);
            }

            foreach (var go in activeTerrainTiles.Keys) {
                SimplePool.Despawn(go);
            }

            activeObjects = new Dictionary<GameObject, TileObject>();
            CurrentRoom = "";
            HasActiveRoom = false;
        }

        private void BuildRoom(string roomKey) {
            CurrentRoom = roomKey;
            var room = RoomAtlas.GetRoom(CurrentRoom);
            HasActiveRoom = true;

            BuildColliders(room.RoomColliders);

            foreach (var tile in room.TileProvider.GetTiles()) {
                BuildTile(tile);
            }

            foreach (var thing in room.TileProvider.GetTileObjects()) {
                BuildThing(thing);
            }

            foreach (var thing in room.TileProvider.GetScriptableTileObjects()) {
                BuildScriptableThing(thing);
            }
        }

        #region Collider Construction

        private void BuildColliders(RoomColliders colliders) {
            BuildEdgeColliders(colliders.EdgePoints);
            BuildBoxColliders(colliders.BoxColliders);
            UpdateCornerPositions(colliders.EdgePoints);
        }

        private void UpdateCornerPositions(IList<Position> colliderPoints) {
            var minX = colliderPoints[0].x;
            var maxX = colliderPoints[0].x;
            var minY = colliderPoints[0].y;
            var maxY = colliderPoints[0].y;
            foreach (var pos in colliderPoints) {
                minX = (pos.x < minX) ? pos.x : minX;
                maxX = (pos.x > maxX) ? pos.x : maxX;
                minY = (pos.y < minY) ? pos.y : minY;
                maxY = (pos.x > maxY) ? pos.y : maxY;
            }

            TopRight = new Position(maxX, maxY);
            BottomLeft = new Position(minY, minX);
            Center = new Position((minX + maxX) / 2, (minY + maxY) / 2);
        }

        private void BuildBoxColliders(IList<BoxColliderInfo> boxColliders) {
            if (boxColliders == null) {
                return;
            }

            IList<BoxCollider2D> colliderComponents = TerrainRoot.GetComponents<BoxCollider2D>().ToList();
            var diff = boxColliders.Count - colliderComponents.Count;
            if (diff > 0) {
                for (int i = 0; i < diff; i++) {
                    colliderComponents.Add(TerrainRoot.AddComponent<BoxCollider2D>());
                }
            }
            else if (diff < 0) {
                for (int i = colliderComponents.Count - 1; i >= boxColliders.Count; i--) {
                    Destroy(colliderComponents[i]);
                    colliderComponents.RemoveAt(i);
                }
            }

            for (int i = 0; i < boxColliders.Count; i++) {
                var currentComponent = colliderComponents[i];
                currentComponent.offset = ApiMappings.Vector2FromPosition(boxColliders[i].Position);
                currentComponent.size = ApiMappings.Vector2FromPosition(boxColliders[i].Size);
            }
        }

        private void BuildEdgeColliders(IList<Position> colliderPoints) {
            EdgeCollider2D colliderRoot = TerrainRoot.GetComponent<EdgeCollider2D>();
            colliderRoot.points = colliderPoints.Select(pos => ApiMappings.Vector2FromPosition(pos)).ToArray();
        }

        #endregion

        #region Tile Construction

        private void BuildTile(Tile tile) {
            Vector3 pos = new Vector3(tile.TilePosition.x, tile.TilePosition.y, 0);
            GameObject poolGo = SimplePool.Spawn(TerrainTilePrefab, pos, Quaternion.identity);
            poolGo.GetComponent<SpriteRenderer>().sprite = ApiMappings.InfoToSprite(tile.Definition.SpriteInfo);
            poolGo.transform.parent = TerrainRoot.transform;
            activeTerrainTiles.Add(poolGo, tile);
        }

        private void BuildThing(TileObject tileObject) {
            var currentThingDef = tileObject.Definition;
            var pos = GetLayeredPosition(tileObject.TilePosition);

            GameObject poolGo = SimplePool.Spawn(ObjectTilePrefab, pos, Quaternion.identity);


            Sprite sprite = ApiMappings.InfoToSprite(currentThingDef.SpriteInfo);
            poolGo.GetComponent<SpriteRenderer>().sprite = sprite;
            var colliderComponent = poolGo.GetComponent<PolygonCollider2D>();
            colliderComponent.enabled = currentThingDef.IsCollidable;
            colliderComponent.isTrigger = false;
            if (currentThingDef.IsCollidable) {
                UpdateColliderPaths(colliderComponent, currentThingDef.SpriteInfo.ColliderShape);
            }

            if (currentThingDef.IsTransparentOnPlayerBehind) {
                if (currentThingDef.SpriteInfo.TransparencyShape != null) {
                    GameObject transparencyGo =
                        SimplePool.Spawn(TransparencyColliderPrefab, pos, Quaternion.identity);
                    UpdateColliderPaths(transparencyGo.GetComponent<PolygonCollider2D>(),
                        currentThingDef.SpriteInfo.TransparencyShape);
                    transparencyGo.transform.parent = poolGo.transform;
                }
            }


            poolGo.transform.parent = ObjectRoot.transform;
            activeObjects[poolGo] = tileObject;
        }

        private void BuildScriptableThing(ScriptableTileObject tileObject) {
            var currentThingDef = tileObject.Definition;
            var pos = new Vector3(tileObject.TilePosition.x, tileObject.TilePosition.y,
                tileObject.TilePosition.y * Constants.ZPositionMultiplier + Constants.ZPositionOffset);
            var poolGo = SimplePool.Spawn(ObjectTilePrefab, pos, Quaternion.identity);
            poolGo.GetComponent<SpriteRenderer>().sprite = ApiMappings.InfoToSprite(currentThingDef.SpriteInfo);
            var colliderComponent = poolGo.GetComponent<PolygonCollider2D>();
            colliderComponent.enabled = currentThingDef.IsCollidable;
            if (currentThingDef.IsCollidable) {
                UpdateColliderPaths(colliderComponent, currentThingDef.SpriteInfo.ColliderShape);
                colliderComponent.isTrigger = currentThingDef.IsTrigger;
            }

            poolGo.transform.parent = ObjectRoot.transform;
            activeObjects[poolGo] = tileObject;
        }

        private Vector3 GetLayeredPosition(TilePosition tilePosition) {
            return new Vector3(tilePosition.x, tilePosition.y,
                tilePosition.y * Constants.ZPositionMultiplier + Constants.ZPositionOffset);
        }

        private void UpdateColliderPaths(PolygonCollider2D colliderComponent, Position[] paths) {
            EmptyPreviousColliders(colliderComponent);

            if (paths != null) {
                CopyNewPaths(colliderComponent, paths);
            }
        }

        private void CopyNewPaths(PolygonCollider2D colliderComponent, Position[] paths) {
            colliderComponent.SetPath(0,
                paths.Select(point => ApiMappings.Vector2FromPosition(point)).ToArray());
        }

        private void EmptyPreviousColliders(PolygonCollider2D colliderComponent) {
            for (var i = 0; i < colliderComponent.pathCount; i++) {
                colliderComponent.SetPath(i, null);
            }
        }

        #endregion
    }
}