using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using NavesPortalforWebWithCoreMvc.Controllers.AuthFromIntranetController;
using NavesPortalforWebWithCoreMvc.Data;
using NavesPortalforWebWithCoreMvc.RfSystemData;
using NuGet.Protocol.Core.Types;
using NavesPortalforWebWithCoreMvc.Models;
using NavesPortalforWebWithCoreMvc.ViewModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.CodeAnalysis;
using NavesPortalforWebWithCoreMvc.Common;
using NavesPortalforWebWithCoreMvc.RfSystemModels;
using Microsoft.Build.Evaluation;

namespace NavesPortalforWebWithCoreMvc.Controllers.DIS
{
    [Authorize]
    [CheckSession]
    public class DisProjectPlanningController : Controller
    {
        private readonly BM_NAVES_PortalContext _repository;
        private readonly RfSystemContext _systemContext;
        private readonly IBM_NAVES_PortalContextProcedures _procedure;

        public DisProjectPlanningController(BM_NAVES_PortalContext repository, RfSystemContext systemContext, IBM_NAVES_PortalContextProcedures procedure)
        {
            _repository = repository;
            _systemContext = systemContext;
            _procedure = procedure;
        }

        public async Task<IActionResult> Registration()
        {
            DisProjectRegistrationViewModel projectRegistrationViewModel = new DisProjectRegistrationViewModel
            {
                PROJECT_INFO = new VNAV_SELECT_DIS_PROJECT_LIST(),
                DIS_PROJECT = new TNAV_DIS_PROJECT(),
                PROJECT_PIC = new List<TNAV_PROJECT_PIC>(),
            };

            ViewBag.ProjectId = await _repository.TNAV_PROJECTs.Where(m => m.PROJECT_TYPE == "DIS").OrderByDescending(m => m.REG_DATE).ToListAsync();

            return View(projectRegistrationViewModel);
        }

        /// <summary>
        /// 상세 보기
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<IActionResult> Detail(Guid? id)
        {
            DisProjectRegistrationViewModel projectRegistrationViewModel;
            ViewBag.ProjectId = await _repository.TNAV_PROJECTs.Where(m => m.PROJECT_TYPE == "DIS").OrderByDescending(m => m.REG_DATE).ToListAsync();

            if (id == null)
            {
                projectRegistrationViewModel = new DisProjectRegistrationViewModel
                {
                    PROJECT_INFO = new VNAV_SELECT_DIS_PROJECT_LIST(),
                    DIS_PROJECT = new TNAV_DIS_PROJECT(),
                    PROJECT_PIC = new List<TNAV_PROJECT_PIC>(),
                };

                return View(projectRegistrationViewModel);
            }

            // 기본 정보
            VNAV_SELECT_DIS_PROJECT_LIST project = _repository.VNAV_SELECT_DIS_PROJECT_LISTs.AsNoTracking().FirstOrDefault(m => m.PROJECT_IDX == id);

            // Registration 정보
            var model = await _repository.TNAV_DIS_PROJECTs.AsNoTracking().FirstOrDefaultAsync(m => m.PROJECT_IDX == id);

            // Project PIC 목록
            List<TNAV_PROJECT_PIC> pics = await WorkingGroupAsync(id);


            projectRegistrationViewModel = new DisProjectRegistrationViewModel
            {
                PROJECT_INFO = project ?? new VNAV_SELECT_DIS_PROJECT_LIST(),
                DIS_PROJECT = model ?? new TNAV_DIS_PROJECT(),
                PROJECT_PIC = pics ?? new List<TNAV_PROJECT_PIC>(),
            };

            ViewBag.WorkScope = await GetProjectInWorksAsync(id);

            return View(projectRegistrationViewModel);
        }

        /// <summary>
        /// 프로젝트에 할당된 Working Group List
        /// </summary>
        /// <param name="id">Project IDX</param>
        /// <returns></returns>
        public async Task<List<TNAV_PROJECT_PIC>> WorkingGroupAsync(Guid? project_Idx)
        {
            return await _repository.TNAV_PROJECT_PICs.AsNoTracking().Where(m => m.PROJECT_IDX == project_Idx).ToListAsync();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="project_Idx"></param>
        /// <returns></returns>
        public async Task<List<PNAV_GET_DIS_PROJECT_IN_WORK_SCOPE_LISTResult>> GetProjectInWorksAsync(Guid? project_Idx)
        {
            return await _procedure.PNAV_GET_DIS_PROJECT_IN_WORK_SCOPE_LISTAsync(project_Idx);
        }

        /// <summary>
        /// Project Registration
        /// </summary>
        /// <param name="models"></param>
        /// <returns></returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(DisProjectRegistrationViewModel models)
        {
            ViewBag.Title = "Project Registration";
            VNAV_SELECT_DIS_PROJECT_LIST project;

            bool isExist = _repository.TNAV_DIS_PROJECTs.Where(m => m.PROJECT_IDX == models.DIS_PROJECT.PROJECT_IDX).Any();
            if (isExist)
            {
                TNAV_DIS_PROJECT Exist_Project = _repository.TNAV_DIS_PROJECTs.Where(m => m.PROJECT_IDX == models.DIS_PROJECT.PROJECT_IDX).First();

                Exist_Project.DISASSEMBLE_PIPING_EST = models.DIS_PROJECT.DISASSEMBLE_PIPING_EST;
                Exist_Project.DISASSEMBLE_PIPING_ACT = models.DIS_PROJECT.DISASSEMBLE_PIPING_ACT;
                Exist_Project.ASSEMBLE_PIPING_EST = models.DIS_PROJECT.ASSEMBLE_PIPING_EST;
                Exist_Project.ASSEMBLE_PIPING_ACT = models.DIS_PROJECT.ASSEMBLE_PIPING_ACT;
                Exist_Project.LEAK_EST = models.DIS_PROJECT.LEAK_EST;
                Exist_Project.LEAK_ACT = models.DIS_PROJECT.LEAK_ACT;
                Exist_Project.FUNC_EST = models.DIS_PROJECT.FUNC_EST;
                Exist_Project.FUNC_ACT = models.DIS_PROJECT.FUNC_ACT;
                Exist_Project.TRIAL_EST = models.DIS_PROJECT.TRIAL_EST;
                Exist_Project.TRIAL_ACT = models.DIS_PROJECT.TRIAL_ACT;
                Exist_Project.MODIFY_DATE = DateTime.Now;
                Exist_Project.MODIFY_USER_NAME = HttpContext.Session.GetString("UserName");
                Exist_Project.IS_DELETED = false;

                // 상태값 저장
                TNAV_COMMON_LOG LogViewModel = new TNAV_COMMON_LOG()
                {
                    REG_DATE = DateTime.Now,
                    USER_NAME = HttpContext.Session.GetString("UserName"),
                    PLATFORM = "DIS",
                    MENU_NAME = "Registration",
                    TARGET_IDX = Exist_Project.PROJECT_IDX,
                    STATUS = CommonSettingData.LogStatus.MODIFY.ToString()
                };
                _repository.Add(LogViewModel);

                _repository.Update(Exist_Project);
                await _repository.SaveChangesAsync();

                return RedirectToAction(nameof(Detail), new { id = Exist_Project.PROJECT_IDX });
            }
            else
            {
                project = _repository.VNAV_SELECT_DIS_PROJECT_LISTs.AsNoTracking().FirstOrDefault(m => m.PROJECT_IDX == models.DIS_PROJECT.PROJECT_IDX);
                TNAV_DIS_PROJECT proj = new TNAV_DIS_PROJECT()
                {
                    PROJECT_ID = project.PROJECT_ID,
                    PROJECT_IDX = project.PROJECT_IDX,
                    PROJECT_TITLE = project.PROJECT_TITLE,
                    DISASSEMBLE_PIPING_EST = models.DIS_PROJECT.DISASSEMBLE_PIPING_EST,
                    DISASSEMBLE_PIPING_ACT = models.DIS_PROJECT.DISASSEMBLE_PIPING_ACT,
                    ASSEMBLE_PIPING_EST = models.DIS_PROJECT.ASSEMBLE_PIPING_EST,
                    ASSEMBLE_PIPING_ACT = models.DIS_PROJECT.ASSEMBLE_PIPING_ACT,
                    STATUS = CommonSettingData.WrokId_Status_Case_DIS_MRO.DRAFT.ToString(),
                    LEAK_EST = models.DIS_PROJECT.LEAK_EST,
                    LEAK_ACT = models.DIS_PROJECT.LEAK_ACT,
                    FUNC_EST = models.DIS_PROJECT.FUNC_EST,
                    FUNC_ACT = models.DIS_PROJECT.FUNC_ACT,
                    TRIAL_EST = models.DIS_PROJECT.TRIAL_EST,
                    TRIAL_ACT = models.DIS_PROJECT.TRIAL_ACT,
                    CREATE_USER_NAME = HttpContext.Session.GetString("UserName")
                };

                // 상태값 저장
                TNAV_COMMON_LOG LogViewModel = new TNAV_COMMON_LOG()
                {
                    REG_DATE = DateTime.Now,
                    USER_NAME = HttpContext.Session.GetString("UserName"),
                    PLATFORM = "DIS",
                    MENU_NAME = "Registration",
                    TARGET_IDX = project.PROJECT_IDX,
                    STATUS = CommonSettingData.LogStatus.CREATE.ToString()
                };
                _repository.Add(LogViewModel);

                _repository.Add(proj);
                await _repository.SaveChangesAsync();

                //return View("Detail", models);
                return RedirectToAction(nameof(Detail), new { id = project.PROJECT_IDX });
            }
        }

        /// <summary>
        /// Planning 화면 이동
        /// </summary>
        /// <param name="id">Work ID</param>
        /// <returns></returns>
        public async Task<IActionResult> Planning()
        {
            ViewBag.NextDate = "--";
            ViewBag.WorkId = await _repository.VNAV_SELECT_PMS_WORKID_LISTs.Where(m => m.PROJECT_TYPE == "DIS").OrderByDescending(m => m.REG_DATE).ToListAsync();

            DisPlanningViewModel models = new DisPlanningViewModel()
            {
                PROJECT_INFO = new VNAV_SELECT_DIS_PLANNING_LIST(),
                DIS_PLANNING = new TNAV_DIS_PLANNING(),
                PROJECT_PIC = new List<TNAV_PROJECT_PIC>()
            };

            return View(models);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="id">WorkID</param>
        /// <returns></returns>
        public async Task<IActionResult> DetailPlanning(Guid? id)
        {
            ViewBag.Title = "Work Planning";
            ViewBag.NextDate = "--";
            ViewBag.WorkId = await _repository.VNAV_SELECT_PMS_WORKID_LISTs.Where(m => m.PROJECT_TYPE == "DIS").OrderByDescending(m => m.REG_DATE).ToListAsync();

            DisPlanningViewModel models;

            if (id == null)
            {
                models = new DisPlanningViewModel()
                {
                    PROJECT_INFO = new VNAV_SELECT_DIS_PLANNING_LIST(),
                    DIS_PLANNING = new TNAV_DIS_PLANNING(),
                    PROJECT_PIC = new List<TNAV_PROJECT_PIC>()
                };
                return View(models);
            }
            else
            {
                VNAV_SELECT_DIS_PLANNING_LIST Project = await _repository.VNAV_SELECT_DIS_PLANNING_LISTs.Where(m => m.WORK_IDX == id).FirstAsync();
                TNAV_DIS_PLANNING planning = await _repository.TNAV_DIS_PLANNINGs.AsNoTracking().FirstOrDefaultAsync(m => m.WORK_IDX == id);
                List<TNAV_PROJECT_PIC> Pic = await _repository.TNAV_PROJECT_PICs.AsNoTracking().Where(m => m.PROJECT_IDX == Project.PROJECT_IDX).ToListAsync();

                List<dropdownViewModel> ddlPic = new List<dropdownViewModel>();
                foreach (TNAV_PROJECT_PIC item in Pic.Where(m => m.PROJECT_POSTION == "PIC"))
                {
                    ddlPic.Add(new dropdownViewModel
                    {
                        Name = item.USRE_NAME_EN + " (" + item.SUR_NO + ")",
                        Value = item.SUR_NO
                    });
                }

                ViewBag.PicList = ddlPic;

                if (planning != null)
                {
                    if (planning.END_DATE != null && planning.NEXT_DATE != null)
                    {
                        ViewBag.NextDate = Convert.ToDateTime(planning.END_DATE).AddYears(Convert.ToInt32(planning.NEXT_DATE)).ToShortDateString();
                    }
                    else
                    {
                        ViewBag.NextDate = "--";
                    }
                }

                models = new DisPlanningViewModel()
                {
                    PROJECT_INFO = Project ?? new VNAV_SELECT_DIS_PLANNING_LIST(),
                    DIS_PLANNING = planning ?? new TNAV_DIS_PLANNING(),
                    PROJECT_PIC = Pic ?? new List<TNAV_PROJECT_PIC>()
                };
            }

            return View(models);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreatePlnnning(DisPlanningViewModel models)
        {
            bool IsExist = _repository.TNAV_DIS_PLANNINGs.Where(m => m.WORK_IDX == models.PROJECT_INFO.WORK_IDX).Any();

            if (IsExist)
            {
                try
                {
                    TNAV_DIS_PLANNING Exist_planning = _repository.TNAV_DIS_PLANNINGs.Where(m => m.WORK_IDX == models.PROJECT_INFO.WORK_IDX).First();
                    Exist_planning.SURVEYOR = models.DIS_PLANNING.SURVEYOR;
                    Exist_planning.START_DATE = models.DIS_PLANNING.START_DATE;
                    Exist_planning.END_DATE = models.DIS_PLANNING.END_DATE;
                    Exist_planning.NEXT_DATE = models.DIS_PLANNING.NEXT_DATE;
                    Exist_planning.MODIFY_DATE = DateTime.Now;
                    Exist_planning.MODIFY_USER_NAME = HttpContext.Session.GetString("UserName");

                    // 상태값 저장
                    TNAV_COMMON_LOG LogViewModel = new TNAV_COMMON_LOG()
                    {
                        REG_DATE = DateTime.Now,
                        USER_NAME = HttpContext.Session.GetString("UserName"),
                        PLATFORM = "DIS",
                        MENU_NAME = "Planning",
                        TARGET_IDX = Exist_planning.WORK_IDX,
                        STATUS = CommonSettingData.LogStatus.MODIFY.ToString()
                    };
                    _repository.Add(LogViewModel);

                    _repository.Update(Exist_planning);
                    await _repository.SaveChangesAsync();

                    return RedirectToAction(nameof(DetailPlanning), new { id = Exist_planning.WORK_IDX });
                }
                catch (Exception e)
                {
                    return RedirectToAction("SaveException", "Error", new { ex = e.InnerException.Message, returnController = "DisProjectPlanning", returnView = "Create" });
                }
            }
            else
            {
                try
                {
                    VNAV_SELECT_DIS_PLANNING_LIST Exist_planning = _repository.VNAV_SELECT_DIS_PLANNING_LISTs.AsNoTracking().FirstOrDefault(m => m.WORK_IDX == models.PROJECT_INFO.WORK_IDX);
                    TNAV_DIS_PLANNING Planning = new TNAV_DIS_PLANNING()
                    {
                        WORK_IDX = Exist_planning.WORK_IDX,
                        WORK_ID = Exist_planning.WORK_ID,
                        PROJECT_IDX = Exist_planning.PROJECT_IDX,
                        PROJECT_ID = Exist_planning.PROJECT_ID,

                        SURVEYOR = models.DIS_PLANNING.SURVEYOR,
                        START_DATE = models.DIS_PLANNING.START_DATE,
                        END_DATE = models.DIS_PLANNING.END_DATE,
                        NEXT_DATE = models.DIS_PLANNING.NEXT_DATE,
                        REG_DATE = DateTime.Now,
                        CREATE_USER_NAME = HttpContext.Session.GetString("UserName")
                    };

                    // 상태값 저장
                    TNAV_COMMON_LOG LogViewModel = new TNAV_COMMON_LOG()
                    {
                        REG_DATE = DateTime.Now,
                        USER_NAME = HttpContext.Session.GetString("UserName"),
                        PLATFORM = "DIS",
                        MENU_NAME = "Planning",
                        TARGET_IDX = Exist_planning.WORK_IDX,
                        STATUS = CommonSettingData.LogStatus.CREATE.ToString()
                    };
                    _repository.Add(LogViewModel);

                    // Project ID 상태값 변경
                    // 개인별 할당 시 In-Progress (Project)                
                    TNAV_PROJECT _PROJECT = _repository.TNAV_PROJECTs.Where(m => m.PROJECT_IDX == Exist_planning.PROJECT_IDX).First();
                    _PROJECT.STATUS = CommonSettingData.ProjectID_Status.INPROGRESS.ToString();
                    _repository.Update(_PROJECT);

                    // DIS 프로젝트 상태 변경
                    TNAV_DIS_PROJECT _DIS_PROJECT = _repository.TNAV_DIS_PROJECTs.Where(m => m.PROJECT_IDX == Exist_planning.PROJECT_IDX).First();
                    _DIS_PROJECT.STATUS = CommonSettingData.WrokId_Status_Case_DIS_MRO.PLAN.ToString();
                    _repository.Update(_DIS_PROJECT);

                    // Planning 저장
                    _repository.Add(Planning);

                    await _repository.SaveChangesAsync();
                    return RedirectToAction(nameof(DetailPlanning), new { id = Exist_planning.WORK_IDX });
                }
                catch (Exception e)
                {
                    return RedirectToAction("SaveException", "Error", new { ex = e.InnerException.Message, returnController = "DisProjectPlanning", returnView = "Create" });
                }
            }
        }

        /// <summary>
        /// Project Id 상세 내용
        /// </summary>
        /// <param name="_projectID"></param>
        /// <returns></returns>
        [HttpPost]
        public JsonResult GetProjectId(string _projectID)
        {
            VNAV_SELECT_PMS_PROJECTID_DETAIL result = new VNAV_SELECT_PMS_PROJECTID_DETAIL();
            try
            {
                if (_projectID.Count() > 2)
                {
                    result = _repository.VNAV_SELECT_PMS_PROJECTID_DETAILs.Where(m => m.PROJECT_ID == _projectID).First();
                    //return Json(result);
                }
                //else
                //{
                //}
                return Json(result);
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}