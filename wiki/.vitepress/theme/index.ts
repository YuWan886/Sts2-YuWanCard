import DefaultTheme from 'vitepress/theme'
import Layout from './Layout.vue'
import CardBrowser from './components/CardBrowser.vue'
import RelicBrowser from './components/RelicBrowser.vue'
import GlobalSearch from './components/GlobalSearch.vue'
import RichDescription from './components/RichDescription.vue'
import EntityGrid from './components/EntityGrid.vue'
import './custom.css'

export default {
  extends: DefaultTheme,
  Layout,
  enhanceApp({ app }) {
    app.component('CardBrowser', CardBrowser)
    app.component('RelicBrowser', RelicBrowser)
    app.component('GlobalSearch', GlobalSearch)
    app.component('RichDescription', RichDescription)
    app.component('EntityGrid', EntityGrid)
  }
}
