#if false
#define SUPPORTS_MODELS
#endif

#if false
#define SUPPORTS_LIGHTS
#endif

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using FlatRedBall.ManagedSpriteGroups;

#if SUPPORTS_MODELS
using FlatRedBall.Graphics.Model;
#endif

using FlatRedBall.Math;
using FlatRedBall.Instructions;
using FlatRedBall.Utilities;

using FlatRedBall.Math.Geometry;


#if FRB_MDX
using Matrix = Microsoft.DirectX.Matrix;
using Vector3 = Microsoft.DirectX.Vector3;
using Rectangle = System.Drawing.Rectangle;
#else
using Matrix = Microsoft.Xna.Framework.Matrix;
using Vector3 = Microsoft.Xna.Framework.Vector3;
using Microsoft.Xna.Framework;

#endif

namespace FlatRedBall.Graphics
{
    #region Enums

    public enum SetCameraOptions
    {
        PerformZRotation,
        ApplyMatrix
    }

    #endregion

    #region Other Classes

    public class LayerCameraSettings
    {
        public float FieldOfView = (float)System.Math.PI / 4.0f;
        public bool Orthogonal = false;
        public float OrthogonalWidth = 800;
        public float OrthogonalHeight = 600;

        public float ExtraRotationZ = 0;

        public float TopDestination = -1;
        public float BottomDestination = -1;
        public float LeftDestination = -1;
        public float RightDestination = -1;


        Vector3 mOldUpVector;

        // We used to have
        // a RotationMatrix
        // but this caused a 
        // bug in Camera rotation.
        // I'm not 100% certain what
        // the issue is, but setting the
        // individual rotaiton values seems
        // to fix it.
        public float RotationX;
        public float RotationY;
        public float RotationZ;

        public LayerCameraSettings Clone()
        {
            return (LayerCameraSettings)this.MemberwiseClone();
        }


        public void SetFromCamera(Camera camera)
        {
            mOldUpVector = camera.UpVector;  
            

            Orthogonal = camera.Orthogonal;
            OrthogonalHeight = camera.OrthogonalHeight;
            OrthogonalWidth = camera.OrthogonalWidth;

            FieldOfView = camera.FieldOfView;

            RotationX = camera.RotationX;
            RotationY = camera.RotationY;
            RotationZ = camera.RotationZ;
        }

        //public void SetCamera(Camera camera, SetCameraOptions options)
        //{

        //}



        public void SetCamera(Camera camera, SetCameraOptions options, LayerCameraSettings lastToModify)
        {
#if !FRB_MDX
            var viewport = camera.GetViewport(this);
            Renderer.GraphicsDevice.Viewport = viewport;
#endif

            camera.Orthogonal = Orthogonal;
            camera.OrthogonalHeight = OrthogonalHeight;
            camera.OrthogonalWidth = OrthogonalWidth;


            if (lastToModify == null)
            {
                // January 11, 2014
                // We used to only do
                // offsets if the Layer
                // was both orthogonal and
                // if its orthogonal width and
                // height matched the destination.
                // Baron runs on multiple resolutions
                // and introduced situations where the
                // ortho width/height may not match the 
                // destination rectangles because we may 
                // scale everything up on larrger resolutions.
                // In that situation we still want to have the layers
                // offset properly.
//                bool offsetOrthogonal = Orthogonal && OrthogonalWidth == RightDestination - LeftDestination &&
  //                      OrthogonalHeight == BottomDestination - TopDestination;

                bool offsetOrthogonal = Orthogonal;

                if (offsetOrthogonal)
                {
                    int differenceX;
                    int differenceY;
                    DetermineOffsetDifference(camera, this, out differenceX, out differenceY);

                    camera.X += differenceX;
                    camera.Y -= differenceY;
                }
            }
            else
            {
                bool offsetOrthogonal = lastToModify.Orthogonal;

                // Undo what we did before
                if (offsetOrthogonal)
                {
                    int differenceX;
                    int differenceY;
                    DetermineOffsetDifference(camera, lastToModify, out differenceX, out differenceY);

                    camera.X -= differenceX;
                    camera.Y += differenceY;
                }

            }

            if (options == SetCameraOptions.ApplyMatrix)
            {
                float rotationZBefore = camera.RotationZ;

                camera.mRotationX = RotationX;
                camera.mRotationY = RotationY;
                camera.RotationZ = RotationZ;

                float rotationZAfter = camera.RotationZ;

                camera.UpVector = mOldUpVector;
            }
            else
            {
                #if !FRB_MDX

                if (ExtraRotationZ != 0)
                {
                    camera.RotationMatrix *= Matrix.CreateFromAxisAngle(camera.RotationMatrix.Backward, ExtraRotationZ);

                }
                camera.UpVector = camera.RotationMatrix.Up;
#endif

            }


            // Set FieldOfView last so it updates the matrices

            camera.FieldOfView = FieldOfView;
        }

        private static void DetermineOffsetDifference(Camera camera, LayerCameraSettings settingsToUse, out int differenceX, out int differenceY)
        {
            int desiredCenterX = camera.DestinationRectangle.Width / 2;
            int actualCenterX = (int)((settingsToUse.LeftDestination + settingsToUse.RightDestination) / 2);
            differenceX = actualCenterX - desiredCenterX;

            if (settingsToUse.RightDestination != settingsToUse.LeftDestination)
            {
                float xDifferenceMultipier = settingsToUse.OrthogonalWidth / (settingsToUse.RightDestination - settingsToUse.LeftDestination);
                differenceX = (int)(differenceX * xDifferenceMultipier);
            }
            else
            {
                differenceX = 0;
            }

            int desiredCenterY = camera.DestinationRectangle.Height / 2;
            int actualCenterY = (int)((settingsToUse.BottomDestination + settingsToUse.TopDestination) / 2);
            differenceY = actualCenterY - desiredCenterY;

            if (settingsToUse.TopDestination != settingsToUse.BottomDestination)
            {
                float yDifferenceMultipier = settingsToUse.OrthogonalHeight / (settingsToUse.BottomDestination - settingsToUse.TopDestination);
                differenceY = (int)(differenceY * yDifferenceMultipier);
            }
            else
            {
                differenceY = 0;
            }

        }

        public void UsePixelCoordinates(Camera valuesToPullFrom)
        {
            Orthogonal = true;
            OrthogonalWidth = valuesToPullFrom.DestinationRectangle.Width;
            OrthogonalHeight = valuesToPullFrom.DestinationRectangle.Height;
        }


        public override string ToString()
        {
            if (Orthogonal)
            {
                return "Orthogonal, Destination(" + 
                    TopDestination +", " + LeftDestination + ", " + BottomDestination + ", " + RightDestination + ")";
            }
            else
            {
                return "Not Orthogonal";
            }
        }
    }

    #endregion

    #region XML Docs
    /// <summary>
    /// Layers are objects which can contain other graphical objects for drawing.  Layers
    /// are used to create specific ordering and can be used to override depth buffer and
    /// z-sorted ordering.
    /// </summary>
    #endregion
    public partial class Layer : INameable, IEquatable<Layer>
    {
        #region Fields

        #region XML Docs
        /// <summary>
        /// List of Sprites that belong to this layer.  Sprites should be added
        /// through SpriteManager.AddToLayer or the AddSprite overloads which 
        /// include a Layer argument.
        /// </summary>
        #endregion
        internal SpriteList mSprites = new SpriteList();
        internal SpriteList mZBufferedSprites = new SpriteList();

        internal PositionedObjectList<Text> mTexts = new PositionedObjectList<Text>();

        internal List<IDrawableBatch> mBatches = new List<IDrawableBatch>();

        ReadOnlyCollection<Sprite> mSpritesReadOnlyCollection;
        ReadOnlyCollection<Sprite> mZBufferedSpritesReadOnly;
        ReadOnlyCollection<Text> mTextsReadOnlyCollection;


        ReadOnlyCollection<IDrawableBatch> mBatchesReadOnlyCollection;

        // internal so the Renderer can access the lists for drawing
        internal PositionedObjectList<AxisAlignedRectangle> mRectangles;
        internal PositionedObjectList<Circle> mCircles;
        internal PositionedObjectList<Polygon> mPolygons;
        internal PositionedObjectList<Line> mLines;
        internal PositionedObjectList<Sphere> mSpheres;
        internal PositionedObjectList<AxisAlignedCube> mCubes;
        internal PositionedObjectList<Capsule2D> mCapsule2Ds;

        //ListBuffer<Sprite> mSpriteBuffer;
        //ListBuffer<Text> mTextBuffer;
        //ListBuffer<PositionedModel> mModelBuffer;
        //ListBuffer<IDrawableBatch> mBatchBuffer;

        bool mVisible;
        bool mRelativeToCamera;

        internal SortType mSortType = SortType.Z;

        internal Camera mCameraBelongingTo = null;

        #region XML Docs
        /// <summary>
        /// Used by the Renderer to override the camera's FieldOfView
        /// when drawing the layer.  This can be used to give each layer
        /// a different field of view.
        /// </summary>
        #endregion
        internal float mOverridingFieldOfView = float.NaN;

        string mName;

        #endregion

        #region Properties

        #region XML Docs
        /// <summary>
        /// The Batches referenced by and drawn on the Layer.
        /// </summary>
        /// <remarks>
        /// The Layer stores a regular IDrawableBatch PositionedObjectList
        /// internally.  Since this internal list is used for drawing
        /// the layer the engine sorts it every frame.  
        /// 
        /// For efficiency purposes the internal IDrawableBatch PositionedObjectList
        /// cannot be sorted.
        /// </remarks>
        #endregion
        public ReadOnlyCollection<IDrawableBatch> Batches
        {
            get { return mBatchesReadOnlyCollection; }
        }

        public LayerCameraSettings LayerCameraSettings
        {
            get;
            set;
        }

        public Camera CameraBelongingTo
        {
            get { return mCameraBelongingTo; }
        }

        public bool IsEmpty
        {
            get
            {

                return mSprites.Count == 0 &&
#if SUPPORTS_MODELS
                    mModels.Count == 0 &&
#endif

                    mTexts.Count == 0 &&
                    mBatches.Count == 0 &&
                    mRectangles.Count == 0 &&        

                    mCircles.Count == 0 &&
                    mPolygons.Count == 0 &&
                    mLines.Count == 0 &&
                    mSpheres.Count == 0 &&
                    mCubes.Count == 0 && 
                    mCapsule2Ds.Count == 0;



            }
        }

        
        public string Name
        {
            get { return mName; }
            set 
            { 
                mName = value;
                mSprites.Name = value + " Sprites";
                mTexts.Name = value + " Texts";

#if SUPPORTS_MODELS
                mModels.Name = value + " Models";
#endif
            }
        }

        //#region XML Docs
        ///// <summary>
        ///// The FieldOfView to use when drawing this Layer.  If the value is
        ///// float.NaN (default) then the Camera's FieldOfView is used.
        ///// </summary>
        //#endregion
        //public float OverridingFieldOfView
        //{
        //    get { return mOverridingFieldOfView; }
        //    set { mOverridingFieldOfView = value; }
        //}


        public bool RelativeToCamera
        {
            get { return mRelativeToCamera; }
            set { mRelativeToCamera = value; }
        }

        #region XML Docs
        /// <summary>
        /// The Sprites referenced by and drawn on the Layer.
        /// </summary>
        /// <remarks>
        /// The Layer stores a regular SpriteList internally.  Since
        /// this internal list is used for drawing the layer the engine
        /// sorts it every frame.  
        /// 
        /// For efficiency purposes the internal SpriteList cannot be sorted.
        /// </remarks>
        #endregion
        public  ReadOnlyCollection<Sprite> Sprites
        {
            get { return mSpritesReadOnlyCollection; }
        }

        public ReadOnlyCollection<Sprite> ZBufferedSprites
        {
            get { return mZBufferedSpritesReadOnly; }
            set { mZBufferedSpritesReadOnly = value; }
        }

        #region XML Docs
        /// <summary>
        /// The Texts referenced by and drawn on the Layer.
        /// </summary>
        /// <remarks>
        /// The Layer stores a regular Text PositionedObjectList
        /// internally.  Since this internal list is used for drawing 
        /// the layer the engine sorts it every frame.  
        /// 
        /// For efficiency purposes the internal Text PositionedObjectList 
        /// cannot be sorted.
        /// </remarks>
        #endregion
        public ReadOnlyCollection<Text> Texts
        {
            get { return mTextsReadOnlyCollection; }
        }

        public IEnumerable<AxisAlignedCube> AxisAlignedCubes
        {
            get
            {
                return mCubes;
            }
        }


        public IEnumerable<AxisAlignedRectangle> AxisAlignedRectangles
        {
            get
            {
                return mRectangles;
            }
        }

        public IEnumerable<Capsule2D> Capsule2Ds
        {
            get
            {
                return mCapsule2Ds;
            }
        }
        
        public IEnumerable<Circle> Circles
        {
            get
            {
                return mCircles;
            }
        }


        public IEnumerable<Line> Lines
        {
            get
            {
                return mLines;
            }
        }


        public IEnumerable<Polygon> Polygons
        {
            get
            {
                return mPolygons;
            }
        }

        public IEnumerable<Sphere> Spheres
        {
            get
            {
                return mSpheres;
            }
        }


        public SortType SortType
        {
            get { return mSortType; }
            set { mSortType = value; }
        }

        #region XML Docs
        /// <summary>
        /// Whether the SpriteLayer is visible.
        /// </summary>
        /// <remarks>
        /// This does not set the contained Sprite's visible value to false.
        /// </remarks>
        #endregion
        public bool Visible
        {
            get { return mVisible; }
            set { mVisible = value; }
        }



        #endregion

        #region Methods

        #region Constructor

        // This used to be internal, but
        // Glue needs to be able to instantiate
        // Layers before adding them to managers
        // so that it can be done in Initialize before
        // we set properties on the Layer like Visible.
        public Layer()
        {
            mSprites.Name = "Layer SpriteList";
            mZBufferedSprites.Name = "Layered ZBuffered SpriteList";

            mTexts.Name = "Layer Text PositionedObjectList";
#if SUPPORTS_MODELS
            mModels.Name = "Layer PositionedModel PositionedObjectList";
#endif

            mSpritesReadOnlyCollection = new ReadOnlyCollection<Sprite>(mSprites);
            mZBufferedSpritesReadOnly = new ReadOnlyCollection<Sprite>(mZBufferedSprites);
            mTextsReadOnlyCollection = new ReadOnlyCollection<Text>(mTexts);

#if SUPPORTS_LIGHTS
            InitializeLights();
#endif

            mBatchesReadOnlyCollection = new ReadOnlyCollection<IDrawableBatch>(mBatches);

            //mSpriteBuffer = new ListBuffer<Sprite>(mSprites);
            //mTextBuffer = new ListBuffer<Text>(mTexts);
            //mModelBuffer = new ListBuffer<PositionedModel>(mModels);
            //mBatchBuffer = new ListBuffer<IDrawableBatch>(mBatches);

            mRectangles = new PositionedObjectList<AxisAlignedRectangle>();
            mRectangles.Name = "Layered AxisAlignedRectangles";

            mCircles = new PositionedObjectList<Circle>();
            mCircles.Name = "Layered Circles";

            mPolygons = new PositionedObjectList<Polygon>();
            mPolygons.Name = "Layered Polygons";

            mLines = new PositionedObjectList<Line>();
            mLines.Name = "Layered Lines";

            mSpheres = new PositionedObjectList<Sphere>();
            mSpheres.Name = "Layered Spheres";

            mCubes = new PositionedObjectList<AxisAlignedCube>();
            mCubes.Name = "Layered Cubes";

            mCapsule2Ds = new PositionedObjectList<Capsule2D>();
            mCapsule2Ds.Name = "Layered Capsule2Ds";
            
            mVisible = true;
        }

        #endregion

        #region Public Methods

        
        public float PixelsPerUnitAt(float absoluteZ)
        {
            Camera camera = Camera.Main;
            if (this.mCameraBelongingTo != null)
            {
                camera = this.mCameraBelongingTo;
            }

            if (this.LayerCameraSettings != null)
            {
                Vector3 position = new Vector3();
                position.Z = absoluteZ;
                return camera.PixelsPerUnitAt(ref position, LayerCameraSettings.FieldOfView, LayerCameraSettings.Orthogonal, LayerCameraSettings.OrthogonalHeight);
            }
            else
            {
                return camera.PixelsPerUnitAt(absoluteZ);
            }
        }

        public void SortYSpritesSecondary()
        {
            mSprites.SortYInsertionDescendingOnZBreaks();
        }

        public void Remove(Sprite spriteToRemove)
        {
            if (spriteToRemove.ListsBelongingTo.Contains(mSprites))
            {
                mSprites.Remove(spriteToRemove);
            }
            else
            {
                mZBufferedSprites.Remove(spriteToRemove);
            }
        }

        public void Remove(SpriteFrame spriteFrame)
        {
            if (spriteFrame.mLayerBelongingTo == this)
            {
                if (spriteFrame.mCenter != null)
                    spriteFrame.mLayerBelongingTo.Remove(spriteFrame.mCenter);
                if (spriteFrame.mTop != null)
                    spriteFrame.mLayerBelongingTo.Remove(spriteFrame.mTop);
                if (spriteFrame.mBottom != null)
                    spriteFrame.mLayerBelongingTo.Remove(spriteFrame.mBottom);
                if (spriteFrame.mLeft != null)
                    spriteFrame.mLayerBelongingTo.Remove(spriteFrame.mLeft);
                if (spriteFrame.mRight != null)
                    spriteFrame.mLayerBelongingTo.Remove(spriteFrame.mRight);
                if (spriteFrame.mTopLeft != null)
                    spriteFrame.mLayerBelongingTo.Remove(spriteFrame.mTopLeft);
                if (spriteFrame.mTopRight != null)
                    spriteFrame.mLayerBelongingTo.Remove(spriteFrame.mTopRight);
                if (spriteFrame.mBottomLeft != null)
                    spriteFrame.mLayerBelongingTo.Remove(spriteFrame.mBottomLeft);
                if (spriteFrame.mBottomRight != null)
                    spriteFrame.mLayerBelongingTo.Remove(spriteFrame.mBottomRight);
            }

        }

        public void Remove(Text textToRemove)
        {
            mTexts.Remove(textToRemove);
        }

        public void Remove(Scene scene)
        {
            for (int i = scene.Sprites.Count-1; i > -1; i--)
            {
                Remove(scene.Sprites[i]);
            }

            for (int i = scene.SpriteGrids.Count - 1; i > -1 ; i--)
            {
                SpriteGrid spriteGrid = scene.SpriteGrids[i];
                spriteGrid.Layer = null;
            }

            #if SUPPORTS_MODELS
            for (int i = scene.PositionedModels.Count - 1; i > -1; i--)
            {
                Remove(scene.PositionedModels[i]);
            }

            #endif

            for (int i = scene.SpriteFrames.Count - 1; i > -1; i--)
            {
                Remove(scene.SpriteFrames[i]);
            }

            for (int i = scene.Texts.Count - 1; i > -1; i--)
            {
                Remove(scene.Texts[i]);
            }
        }

        public void Remove(IDrawableBatch batchToRemove)
        {
            mBatches.Remove(batchToRemove);
        }

        public void SetLayer(Layer otherLayerToSetPropertiesOn)
        {
            mSortType = otherLayerToSetPropertiesOn.SortType;
            mOverridingFieldOfView = otherLayerToSetPropertiesOn.mOverridingFieldOfView;


            if (otherLayerToSetPropertiesOn.LayerCameraSettings == null)
            {
                LayerCameraSettings = null;
            }
            else
            {
                LayerCameraSettings = otherLayerToSetPropertiesOn.LayerCameraSettings.Clone();

            }
        }

        public override string ToString()
        {
            return "Name: " + mName;
        }

        public void UsePixelCoordinates()
        {
            if (LayerCameraSettings == null)
            {
                LayerCameraSettings = new LayerCameraSettings();
            }

            if (mCameraBelongingTo != null)
            {
                LayerCameraSettings.UsePixelCoordinates(mCameraBelongingTo);
            }
            else
            {
                LayerCameraSettings.UsePixelCoordinates(SpriteManager.Camera);
            }
        }

        public void WorldToLayerCoordinates(float worldX, float worldY, float worldZ, out float xOnLayer, out float yOnLayer)
        {
            xOnLayer = 0;
            yOnLayer = 0;
            int screenX = 0;
            int screenY = 0;

            MathFunctions.AbsoluteToWindow(worldX, worldY, worldZ, ref screenX, ref screenY, SpriteManager.Camera);

            if(this.LayerCameraSettings == null)
            {
                MathFunctions.WindowToAbsolute(screenX, screenY, ref xOnLayer, ref yOnLayer, worldZ, SpriteManager.Camera, Camera.CoordinateRelativity.RelativeToWorld);

            }
            else
            {
                float xEdge = 0;
                float yEdge = 0;

                if (LayerCameraSettings.Orthogonal)
                {
                    xEdge = LayerCameraSettings.OrthogonalWidth / 2.0f;
                    yEdge = LayerCameraSettings.OrthogonalHeight / 2.0f;
                }
                else
                {
                    xEdge = (float)(100 * System.Math.Tan(LayerCameraSettings.FieldOfView / 2.0));
                    // Right now we just assume the same aspect ratio, but we may want the LayerCamerasettings
                    // to have its own AspectRatio - if so, this needs to be modified
                    yEdge = (float)(xEdge * SpriteManager.Camera.AspectRatio);
                }

                
                Rectangle destinationRectangle;

                if (LayerCameraSettings.LeftDestination > 0 && LayerCameraSettings.TopDestination > 0)
                {
                    destinationRectangle = new Rectangle(
                        MathFunctions.RoundToInt(LayerCameraSettings.LeftDestination),
                        MathFunctions.RoundToInt(LayerCameraSettings.RightDestination),
                        MathFunctions.RoundToInt(LayerCameraSettings.RightDestination - LayerCameraSettings.LeftDestination),
                        MathFunctions.RoundToInt(LayerCameraSettings.BottomDestination - LayerCameraSettings.TopDestination));
                }
                else
                {
                    destinationRectangle = SpriteManager.Camera.DestinationRectangle;
                }

                if (LayerCameraSettings.Orthogonal)
                {
                    Camera camera = SpriteManager.Camera;

                    float top = camera.Y + LayerCameraSettings.OrthogonalHeight/2.0f;
                    float left = camera.X - LayerCameraSettings.OrthogonalWidth/2.0f;

                    float distanceFromLeft = LayerCameraSettings.OrthogonalWidth * screenX / (float)camera.DestinationRectangle.Width;
                    float distanceFromTop = -LayerCameraSettings.OrthogonalHeight * screenY / (float)camera.DestinationRectangle.Height;

                    xOnLayer = left + distanceFromLeft;
                    yOnLayer = top + distanceFromTop;
                }
                else
                {


                    MathFunctions.WindowToAbsolute(screenX, screenY,
                        ref xOnLayer, ref yOnLayer, worldZ,
                        SpriteManager.Camera.Position,
                        xEdge,
                        yEdge,
                        destinationRectangle,
                        Camera.CoordinateRelativity.RelativeToWorld);
                }
            }

        }

        #endregion

        #region Internal Methods

        internal void Add(Sprite sprite)
        {
            if (sprite.mOrdered)
            {
#if DEBUG
                if (mSprites.Contains(sprite))
                {
                    throw new InvalidOperationException("Can't add the Sprite to this layer because it's already added");
                }
#endif
                mSprites.Add(sprite);
            }
            else
            {
#if DEBUG
                if (mZBufferedSprites.Contains(sprite))
                {
                    throw new InvalidOperationException("Can't add the Sprite to this layer because it's already added");
                }
#endif
                mZBufferedSprites.Add(sprite);
            }
        }

        internal void Add(Text text)
        {
#if DEBUG
            if (text.mListsBelongingTo.Contains(mTexts))
            {
                throw new InvalidOperationException("This text is already part of this layer.");
            }
#endif
            mTexts.Add(text);
        }
        
        internal void Add(IDrawableBatch drawableBatch)
        {
            mBatches.Add(drawableBatch);
        }

        //internal void Flush()
        //{
        //    mSpriteBuffer.Flush();
        //    mTextBuffer.Flush();
        //    mModelBuffer.Flush();
        //    mBatchBuffer.Flush();

        //}

        #endregion

        #endregion


        #region IEquatable<Layer> Members

        bool IEquatable<Layer>.Equals(Layer other)
        {
            return this == other;
        }

        #endregion
    }
}