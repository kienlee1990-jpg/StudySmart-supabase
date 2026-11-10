import { createApp } from "vue"
import App from "./App.vue"
import router from "./router"
import "./style.css"
import 'bootstrap/dist/css/bootstrap.min.css';
import 'bootstrap/dist/js/bootstrap.bundle.min.js';
import './styles/subject-theme.css'
import './composables/useAppearance'
createApp(App).use(router).mount('#app')
