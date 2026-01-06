using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using TEngine;
using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// HFSM 上下文，用于状态间传递数据和控制转换
    /// </summary>
    public class HFContext
    {
        // 状态转换标志
        public bool WantEnterGame { get; set; }      // 主菜单 → 游戏
        public bool WantOpenBag { get; set; }        // Normal → OpenBag
        public bool WantCloseBag { get; set; }       // OpenBag → Normal

        // 依赖实例（由状态管理生命周期）
        public BagModel BagModel { get; set; }
        public GameBagSystem BagSystem { get; set; }
    }

    /// <summary>
    /// 游戏状态（复合状态，包含 Normal 和 OpenBag 子状态）
    /// </summary>
    public class GameState : ComposeState<HFContext>
    {
        protected override void OnEnter(HFContext ctx)
        {
            Debug.Log("进入 GameState");
            // 监听场景加载完成事件
            UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
        }

        protected override void OnExit(HFContext ctx)
        {
            Debug.Log("退出 GameState");
            // 移除监听
            UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
        {
            Debug.Log($"[GameState] 场景 {scene.name} 加载完成，设置相机堆栈");
            // 延迟一帧确保所有对象都初始化完成
            SetupCameraStackDelayed().Forget();
        }

        private async UniTaskVoid SetupCameraStackDelayed()
        {
            // 等待一帧，确保场景完全初始化
            await UniTask.Yield();

            var mainCamera = Camera.main;
            if (mainCamera == null)
            {
                Debug.LogWarning("[GameState] 未找到 Main Camera，跳过相机堆栈设置");
                return;
            }

            var uiCamera = UIModule.Instance.UICamera;
            if (uiCamera == null)
            {
                Debug.LogWarning("[GameState] 未找到 UI Camera，跳过相机堆栈设置");
                return;
            }

            // URP camera stack support
            var urpAdditionalDataType = System.Type.GetType(
                "UnityEngine.Rendering.Universal.UniversalAdditionalCameraData, Unity.RenderPipelines.Universal.Runtime");
            if (urpAdditionalDataType == null)
            {
                Debug.Log("[GameState] 非 URP 项目，跳过相机堆栈设置");
                return;
            }

            var additionalData = mainCamera.GetComponent(urpAdditionalDataType);
            if (additionalData == null)
            {
                Debug.LogWarning("[GameState] Main Camera 没有 UniversalAdditionalCameraData 组件");
                return;
            }

            var cameraStackProperty = urpAdditionalDataType.GetProperty("cameraStack");
            if (cameraStackProperty == null) return;

            var cameraStack = cameraStackProperty.GetValue(additionalData) as System.Collections.IList;
            if (cameraStack == null) return;

            if (!cameraStack.Contains(uiCamera))
            {
                cameraStack.Add(uiCamera);
                Debug.Log("[GameState] 相机堆栈设置完成");
            }
            else
            {
                Debug.Log("[GameState] UI Camera 已在堆栈中");
            }
        }
    }

    /// <summary>
    /// Normal 状态：游戏中的默认状态
    /// </summary>
    public class NormalState : LeafState<HFContext>
    {
        protected override void OnEnter(HFContext ctx)
        {
            Debug.Log("进入 NormalState（游戏正常状态）");
            // 清除转换标志
            ctx.WantOpenBag = false;
            GameModule.UI.ShowUIAsync<GameUI>();
        }

        protected override void OnExit(HFContext ctx)
        {
            Debug.Log("退出 NormalState");
        }

        protected override void OnUpdate(HFContext ctx)
        {
            // 你可以在这里添加检测输入的逻辑
            // 例如：按 B 键打开背包
            // if (Input.GetKeyDown(KeyCode.B))
            // {
            //     ctx.WantOpenBag = true;
            // }
        }
    }

    /// <summary>
    /// HFSM 模块接口
    /// </summary>
    public interface IHotFixHFSMModule
    {
        void RequestEnterGame();
        void RequestOpenBag();
        void RequestCloseBag();
    }

    /// <summary>
    /// HFSM 主模块
    /// </summary>
    public class HotFixHFSMModule : Module, IUpdateModule, IHotFixHFSMModule
    {
        public static HotFixHFSMModule Instance { get; private set; }

        public ComposeState<HFContext> HFStateMachine;
        public HFContext HFContext = new HFContext();

        // 暴露状态引用，方便外部请求转换
        private State<HFContext> _mainMenuState;
        private ComposeState<HFContext> _gameState;
        private State<HFContext> _normalState;
        private State<HFContext> _openBagState;

        public override void OnInit()
        {
            Instance = this;

            MainMenuState mainMenuState;
            GameState gameState;
            NormalState normalState;
            OpenBagState openBagState;

            // 正确的 HFSM 构建方式：所有状态放在根 Compose 内
            HFStateMachine = HFSMBuilder<HFContext>.Create()
                .BeginCompose(out var rootState)
                    // 主菜单状态
                    .Leaf<MainMenuState>(out mainMenuState)

                    // 游戏状态（复合）
                    .BeginCompose<GameState>(out gameState)
                        // Normal 子状态
                        .Leaf<NormalState>(out normalState)
                        // OpenBag 子状态
                        .Leaf<OpenBagState>(out openBagState)
                        // 子状态转换: Normal ↔ OpenBag
                        .Transition(normalState, openBagState, ctx => ctx.WantOpenBag)
                        .Transition(openBagState, normalState, ctx => ctx.WantCloseBag)
                        // GameState 默认进入 Normal
                        .Initial(normalState)
                    .EndCompose()

                    // 根层转换: MainMenu ↔ Game
                    .Transition(mainMenuState, gameState, ctx => ctx.WantEnterGame)
                    // 根层默认进入 MainMenu
                    .Initial(mainMenuState)
                .EndCompose()
                .Build();

            // 保存引用
            _mainMenuState = mainMenuState;
            _gameState = gameState;
            _normalState = normalState;
            _openBagState = openBagState;

            // 启动状态机（进入初始状态）
            HFStateMachine.Enter(HFContext);

            Debug.Log("HotFixHFSMModule 初始化完成");
        }

        public void Update(float elapseSeconds, float realElapseSeconds)
        {
            HFStateMachine?.Update(HFContext);
        }

        /// <summary>
        /// 获取当前状态名称（用于调试）
        /// </summary>
        public string GetCurrentStateName()
        {
            try
            {
                var leafState = HFStateMachine?.CurrentLeafState;
                return leafState?.Name ?? "None";
            }
            catch
            {
                return "Unknown";
            }
        }

        /// <summary>
        /// 打印当前状态（调试用）
        /// </summary>
        public void LogCurrentState()
        {
            Debug.Log($"[HFSM] 当前状态: {GetCurrentStateName()}");
        }

        public override void Shutdown()
        {
            HFStateMachine?.Exit(HFContext);
            Instance = null;
        }

        #region 外部调用接口

        /// <summary>
        /// 请求进入游戏（从主菜单）
        /// </summary>
        public void RequestEnterGame()
        {
            Debug.Log($"[HFSM] RequestEnterGame 被调用，当前状态: {GetCurrentStateName()}");
            HFContext.WantEnterGame = true;
            Debug.Log($"[HFSM] WantEnterGame = {HFContext.WantEnterGame}");
        }

        /// <summary>
        /// 请求打开背包
        /// </summary>
        public void RequestOpenBag()
        {
            HFContext.WantOpenBag = true;
        }

        /// <summary>
        /// 请求关闭背包
        /// </summary>
        public void RequestCloseBag()
        {
            HFContext.WantCloseBag = true;
        }

        #endregion
    }
}
