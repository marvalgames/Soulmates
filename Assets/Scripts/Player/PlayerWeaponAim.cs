using Rewired;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Sandbox.Player
{
    public enum WeaponMotion
    {
        None,
        Started,
        Raised,
        Lowering
    }

    public enum CameraTypes
    {
        TopDown,
        TwoD,
        ThirdPerson
    }

    public enum HitPointType
    {
        None,
        Enemy,
        Breakable
    }

    public enum CrossHairLocations
    {
        False,
        Neutral,
        True,
        InCloseLeftToRight, // cursor coming in from left to right
        InCloseRightToLeft,
        InCloseFrontToBack,
        InCloseBackToFront
    }

    public struct ActorWeaponAimComponent : IComponentData
    {
        public WeaponMotion weaponRaised;
        public float weaponUpTimer;
        public CameraTypes weaponCamera;
        public float3 aimDirection;
        public float3 crosshairRaycastTarget;
        public float3 weaponLocation;
        public float3 mousePosition;
        public float3 targetPosition;
        public LocalTransform target;
        public float3 rayCastStart;
        public float3 rayCastEnd;
        public LocalTransform AmmoStartTransform;
        public bool isMouseMoving;
        public float angleToTarget;
        public bool aimMode;
        public bool combatMode;
        public float distanceFromTarget;
        public bool startDashAimMode;
        public bool aimDisabled;
    }

    public class PlayerWeaponAim : MonoBehaviour
    {
        private EntityManager _manager;
        private Entity _entity;
        private Camera _cam;
        private Vector3 _aimCrosshair;


        public bool aimMode = true;
        public bool aimDisabled = false;
        public Transform target;
        public Transform aimTransform;
        [Range(0.0f, 1.0f)] [SerializeField] private float aimWeight = 1.0f;
        [Range(0.0f, 1.0f)] [SerializeField] private float clampWeight = 0.1f;
        [Range(0.0f, 1.0f)] [SerializeField] private float lookWeight = 1f;
        [Header("Aim Speeds")] public float rotateSpeed = 1;
        public float turnSpeed = 1;
        [Header("")] [SerializeField] private float lerpSpeed = 3;
        private float _startAimWeight;
        [SerializeField] private float currentAimWeight;
        [SerializeField] private float targetAimWeight;
        private float _startClampWeight;

        private float _startLookWeight;

        //private float aimLerp = .03f;
        [HideInInspector] public Rewired.Player Player;
        [HideInInspector] public int playerId; // The Rewired player id of this character
        [SerializeField] private Transform crossHair;
        [SerializeField] private Image crosshairImage;
        //[SerializeField] private bool topDownTargeting = false;
        [Range(0.0f, 100.0f)] public float cameraZ = 50f;
        public CameraTypes weaponCamera;
        [SerializeField] private bool simController = false;
        [SerializeField] [Range(0.0f, 100.0f)] private float gamePadSensitivity = 20;
        //[SerializeField] [Range(0.0f, 100.0f)] private float mouseSensitivity = 20;

        //[SerializeField] private float topDownY = 1;
        [Range(80f, 100.0f)] [SerializeField] private float viewportPct = 90;
        [SerializeField] private Vector3 mousePosition;

        public Vector3 lastMousePosition;
        public float3 aimDir;
        [HideInInspector] public Animator animator;
        private float _xMin;
        private float _xMax;
        private float _yMin;
        private float _yMax;
        public Transform playerWeaponLocation;


        private Vector3 _targetPosition = Vector3.zero;
        private Vector3 _worldPosition = Vector3.zero;
        private float3 _closetEnemyWeaponTargetPosition;
    
        public float targetRange = 100;

        private float _combatLayerWeight = 0;
        private static readonly int WeaponRaised = Animator.StringToHash("WeaponRaised");

        private void Start()
        {
            if (!ReInput.isReady) return;


            Player = ReInput.players.GetPlayer(playerId);
            animator = GetComponent<Animator>();
            target = crossHair; //default target

            _cam = Camera.main;

            _startAimWeight = aimWeight;
            _startClampWeight = clampWeight;
            _startLookWeight = lookWeight;
            currentAimWeight = aimWeight;
            targetAimWeight = _startAimWeight;

            SetCursorBounds();

            if (_entity == Entity.Null)
            {
                _entity = GetComponent<CharacterEntityTracker>().linkedEntity;
                if (_manager == default)
                {
                    _manager = GetComponent<CharacterEntityTracker>().entityManager;
                }
                if (_entity != Entity.Null)
                {
                    _manager.AddComponentObject(_entity, this);



                    _manager.AddComponentData(_entity,
                        new ActorWeaponAimComponent
                        {
                            aimMode = aimMode,
                            weaponCamera = weaponCamera,
                            crosshairRaycastTarget =
                                new float3
                                {
                                    x = transform.position.x, y = transform.position.y, z = transform.position.z
                                }
                        });
                }
            }
        }

        private void SetCursorBounds()
        {
            mousePosition = new Vector2(Screen.width / 2f, Screen.height * .75f);
            _xMin = Screen.width * (1 - viewportPct / 100);
            _xMax = Screen.width * viewportPct / 100;
            _yMin = Screen.height * (1 - viewportPct / 100);
            _yMax = Screen.height * viewportPct / 100;

        }


        public void SetAim()
        {
            var transform1 = crossHair.transform;
            var position1 = transform1.position;
            var aimTarget = position1;
            var cam2d = weaponCamera == CameraTypes.TwoD;
            if (cam2d)
            {
                var position = crossHair.transform.position;
                var xd = math.sign(position.x) * 50;
                var yd = math.sign(position.y) * 50;

                aimTarget = new Vector3(position.x + xd, position.y + yd,
                    position.z);
            }

          

            
        }

        public void SetIK()
        {
         
        }


        private void Crosshair(RoleReversalMode roleReversal)
        {
            var actorWeaponAimComponent = _manager.GetComponentData<ActorWeaponAimComponent>(_entity);
            crosshairImage.enabled = true;
            aimMode = actorWeaponAimComponent.aimMode;
            aimDisabled = actorWeaponAimComponent.aimDisabled;

            if (crossHair == null || !actorWeaponAimComponent.aimMode)
            {
                crosshairImage.enabled = false;
                return;
            }
           
            actorWeaponAimComponent.weaponLocation = playerWeaponLocation.position;
            var controller = Player.controllers.GetLastActiveController();
            if (controller == null && simController == false) return;
            float x, y, z;
            var gamePad = false;
            if (controller != null)
            {
                if (controller.type == ControllerType.Joystick) gamePad = true;
            }
            //Debug.Log("MOUSE1 " + mousePosition);
            if (simController) gamePad = true;
            float3 position = transform.position;
            float3 playerScreen = _cam.WorldToScreenPoint(position);
            var playerToMouseDir = (float3)mousePosition - playerScreen;
            bool behind = math.dot(playerToMouseDir, transform.forward) < 0;
            _aimCrosshair = Vector3.zero;
            x = Player.GetAxis("RightHorizontal");
            if (math.abs(x) < .000001) x = 0;
            y = Player.GetAxis("RightVertical");
            if (math.abs(y) < .000001) y = 0;

            var aim = new Vector3(
                x * Time.deltaTime ,
                y * Time.deltaTime ,
                0
            );

            aim.Normalize();

            _aimCrosshair = aim;

            if (gamePad)
            {
                mousePosition += new Vector3(_aimCrosshair.x * gamePadSensitivity, _aimCrosshair.y * gamePadSensitivity, 0);
            }
            else
            {
                mousePosition = Player.controllers.Mouse.screenPosition;
            }


            if (weaponCamera == CameraTypes.ThirdPerson)
            {
                mousePosition.z = actorWeaponAimComponent.crosshairRaycastTarget.z - _cam.transform.position.z;
                _worldPosition = _cam.ScreenToWorldPoint(mousePosition);
                x = _worldPosition.x;
                y = _worldPosition.y;
                z = _worldPosition.z;

                _targetPosition = new Vector3(
                    x,
                    y,
                    z
                );
            }


            if (mousePosition.x < _xMin) mousePosition.x = _xMin;
            if (mousePosition.x > _xMax) mousePosition.x = _xMax/2;
            if (mousePosition.y < _yMin) mousePosition.y = _yMin;
            if (mousePosition.y > _yMax) mousePosition.y = _yMax/2;

            crosshairImage.transform.position = mousePosition; //*********************
            actorWeaponAimComponent.mousePosition = mousePosition;
            actorWeaponAimComponent.weaponCamera = weaponCamera;
            var ray = _cam.ScreenPointToRay(mousePosition);
            float3 start = _cam.ScreenToWorldPoint(new Vector3(mousePosition.x, mousePosition.y, 0));
            float3 end = ray.origin + Vector3.Normalize(ray.direction) * targetRange;
            //Debug.DrawRay(start, Vector3.Normalize(ray.direction) * targetRange, Color.yellow, SystemAPI.Time.DeltaTime);


            actorWeaponAimComponent.rayCastStart = start;
            actorWeaponAimComponent.rayCastEnd = end;
            actorWeaponAimComponent.targetPosition = _targetPosition;
            actorWeaponAimComponent.isMouseMoving = false;

            var currentMousePosition = mousePosition;
            currentMousePosition.z = 0;
            if (math.distancesq(currentMousePosition, lastMousePosition) > .00001)
            {
                actorWeaponAimComponent.isMouseMoving = true;
                actorWeaponAimComponent.weaponRaised = WeaponMotion.Lowering;
                animator.SetInteger(WeaponRaised, 3);
                animator.SetLayerWeight(1, 1);
            }

            lastMousePosition = mousePosition;
           
            lastMousePosition.z = 0;

            _manager.SetComponentData(_entity, actorWeaponAimComponent);

            if (roleReversal == RoleReversalMode.On) crosshairImage.enabled = false;
        }

        public void LateUpdateSystem(WeaponMotion weaponMotion)
        {
            if (_entity == Entity.Null) return;
            var hasComponent = _manager.HasComponent<ActorWeaponAimComponent>(_entity) &&
                               _manager.HasComponent<ApplyImpulseComponent>(_entity) && _manager.HasComponent<WeaponComponent>(_entity) ;
            if (hasComponent == false) return;
            var roleReverse = _manager.GetComponentData<WeaponComponent>(_entity).roleReversal;
            Crosshair(roleReverse);
            aimWeight = _startAimWeight;
            clampWeight = _startClampWeight;

            if (targetAimWeight != 0)
            {
                lookWeight = _startLookWeight;
            }

            _targetPosition.x = _manager.GetComponentData<ActorWeaponAimComponent>(_entity).crosshairRaycastTarget.x;
            _targetPosition.z = _manager.GetComponentData<ActorWeaponAimComponent>(_entity).crosshairRaycastTarget.z;
            _targetPosition.y = _manager.GetComponentData<ActorWeaponAimComponent>(_entity).crosshairRaycastTarget.y;
            var aimTarget = _targetPosition;

            aimDir = math.normalize(aimTarget - playerWeaponLocation.position);
            SetAim();
            SetIK();
        }

    }
}