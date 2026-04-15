import { createRouter, createWebHistory } from 'vue-router'
import { useAuthStore } from '@/stores/auth'

const routes = [
  // App shell — all routes require auth (handled by Keycloak at app init)
  {
    path: '/',
    component: () => import('@/components/layout/AppLayout.vue'),
    children: [
      { path: '',          redirect: '/dashboard' },
      { path: 'dashboard', name: 'Dashboard', component: () => import('@/views/dashboard/DashboardView.vue') },

      // Courses
      { path: 'courses',                      name: 'CourseList',   component: () => import('@/views/courses/CourseListView.vue') },
      { path: 'courses/create',               name: 'CourseCreate', component: () => import('@/views/courses/CourseFormView.vue'), meta: { role: 'teacher' } },
      { path: 'courses/:id',                  name: 'CourseDetail', component: () => import('@/views/courses/CourseDetailView.vue') },
      { path: 'courses/:id/edit',             name: 'CourseEdit',   component: () => import('@/views/courses/CourseFormView.vue'),   meta: { role: 'teacher' } },

      // Modules
      { path: 'courses/:courseId/modules/:id',        name: 'ModuleView',   component: () => import('@/views/modules/ModuleView.vue') },
      { path: 'courses/:courseId/modules/create',     name: 'ModuleCreate', component: () => import('@/views/modules/ModuleFormView.vue'), meta: { role: 'teacher' } },
      { path: 'courses/:courseId/modules/:id/edit',   name: 'ModuleEdit',   component: () => import('@/views/modules/ModuleFormView.vue'), meta: { role: 'teacher' } },

      // Assignments
      { path: 'courses/:courseId/assignments/:id',             name: 'AssignmentView',        component: () => import('@/views/assignments/AssignmentView.vue') },
      { path: 'courses/:courseId/assignments/:id/submissions', name: 'AssignmentSubmissions', component: () => import('@/views/assignments/SubmissionsView.vue'), meta: { role: 'teacher' } },

      // Quiz
      { path: 'courses/:courseId/quizzes/:id',        name: 'QuizDetail', component: () => import('@/views/quiz/QuizDetailView.vue') },
      { path: 'courses/:courseId/quizzes/:id/take',   name: 'QuizTake',   component: () => import('@/views/quiz/QuizTakeView.vue') },
      { path: 'attempts/:attemptId/result',           name: 'QuizResult', component: () => import('@/views/quiz/QuizResultView.vue') },
      { path: 'courses/:courseId/quizzes/:id/manage', name: 'QuizManage', component: () => import('@/views/quiz/QuizManageView.vue'), meta: { role: 'teacher' } },

      // Forum
      { path: 'courses/:courseId/forum',               name: 'Forum',       component: () => import('@/views/forum/ForumView.vue') },
      { path: 'courses/:courseId/forum/create',         name: 'ForumCreate', component: () => import('@/views/forum/ForumCreateView.vue') },
      { path: 'courses/:courseId/forum/:threadId(\\d+)', name: 'ForumThread', component: () => import('@/views/forum/ThreadView.vue') },

      // Gradebook
      { path: 'courses/:courseId/gradebook', name: 'Gradebook', component: () => import('@/views/gradebook/GradebookView.vue') },

      // Certificates & Completion
      { path: 'courses/:courseId/certificate', name: 'Certificate', component: () => import('@/views/certificates/CertificateView.vue') },

      // Calendar
      { path: 'calendar', name: 'Calendar', component: () => import('@/views/calendar/CalendarView.vue') },

      // Notifications
      { path: 'notifications', name: 'Notifications', component: () => import('@/views/notifications/NotificationsView.vue') },

      // Admin
      { path: 'admin',         name: 'Admin',        component: () => import('@/views/admin/AdminDashboard.vue'), meta: { role: 'admin' } },
      { path: 'admin/users',   name: 'AdminUsers',   component: () => import('@/views/admin/AdminUsers.vue'),     meta: { role: 'admin' } },
      { path: 'admin/courses', name: 'AdminCourses', component: () => import('@/views/admin/AdminCourses.vue'),   meta: { role: 'admin' } },

      // Question Bank
      { path: 'question-bank', name: 'QuestionBank', component: () => import('@/views/quiz/QuestionBankView.vue'), meta: { role: 'teacher' } },

      // Messaging
      { path: 'messages', name: 'Messages', component: () => import('@/views/messages/MessagingView.vue') },

      // Activity
      { path: 'activity', name: 'Activity', component: () => import('@/views/activity/ActivityView.vue') },

      // Practice Quiz (standalone — tanpa enroll kursus)
      { path: 'practice',                          name: 'PracticeList',   component: () => import('@/views/practice/PracticeListView.vue') },
      { path: 'practice/attempt/:attemptId',        name: 'PracticeTake',   component: () => import('@/views/practice/PracticeTakeView.vue') },
      { path: 'practice/result/:attemptId',         name: 'PracticeResult', component: () => import('@/views/practice/PracticeResultView.vue') },

      // Ujian (Question Set)
      { path: 'ujian',                                  name: 'UjianList',    component: () => import('@/views/questionsets/QuestionSetListView.vue') },
      { path: 'ujian/create',                           name: 'UjianCreate',  component: () => import('@/views/questionsets/QuestionSetManageView.vue'), meta: { role: 'teacher' } },
      { path: 'ujian/:id/manage',                       name: 'UjianManage',  component: () => import('@/views/questionsets/QuestionSetManageView.vue'), meta: { role: 'teacher' } },
      { path: 'ujian/:id/take',                         name: 'UjianTake',    component: () => import('@/views/questionsets/QuestionSetTakeView.vue') },
      { path: 'ujian/:id/grading',                      name: 'UjianGrading', component: () => import('@/views/questionsets/QuestionSetGradingView.vue'), meta: { role: 'teacher' } },
      { path: 'ujian-attempts/:attemptId/result',       name: 'UjianResult',  component: () => import('@/views/questionsets/QuestionSetResultView.vue') },
    ]
  },

  { path: '/:pathMatch(.*)*', redirect: '/' }
]

const router = createRouter({
  history: createWebHistory(),
  routes,
  scrollBehavior: () => ({ top: 0 })
})

router.beforeEach((to, _from, next) => {
  if (to.meta.role) {
    const auth = useAuthStore()
    const allowed = to.meta.role === 'teacher'
      ? auth.isTeacher
      : to.meta.role === 'admin'
        ? auth.isAdmin
        : true
    if (!allowed) return next({ name: 'Dashboard' })
  }
  next()
})

export default router
