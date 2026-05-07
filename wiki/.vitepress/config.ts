import { defineConfig } from 'vitepress'

const SITE_URL = 'https://yuwan886.github.io'
const OG_IMAGE = '/images/characters/pig.png'
const GITHUB_REPO = 'https://github.com/YuWan886/Sts2-YuWanCard'

const sharedThemeConfig = {
  search: {
    provider: 'local' as const,
    options: {
      locales: {
        zh: {
          translations: {
            button: { buttonText: '搜索文档', buttonAriaLabel: '搜索文档' },
            modal: {
              noResultsText: '无法找到相关结果',
              resetButtonTitle: '清除查询条件',
              footer: { selectText: '选择', navigateText: '切换' }
            }
          }
        }
      }
    }
  },

  socialLinks: [
    { icon: 'github' as const, link: GITHUB_REPO }
  ],

  footer: {
    message: 'YuWanCard — Slay the Spire 2 Pig Character Mod',
    copyright: `Copyright © 2024-${new Date().getFullYear()} YuWanCard`
  },

  lastUpdated: {
    text: '最后更新于',
    formatOptions: { dateStyle: 'short' as const, timeStyle: 'medium' as const }
  },

  editLink: {
    pattern: `${GITHUB_REPO}/edit/main/wiki/:path`,
    text: '在 GitHub 编辑此页'
  },

  outline: { level: [2, 3] as [number, number] }
}

export default defineConfig({
  lang: 'zh-CN',
  title: 'YuWanCard Wiki',
  description: 'Slay the Spire 2 Pig Character Mod Documentation',

  head: [
    ['link', { rel: 'icon', href: '/images/characters/pig.png' }],
    ['meta', { name: 'theme-color', content: '#0a1118' }],
    ['meta', { property: 'og:type', content: 'website' }],
    ['meta', { property: 'og:site_name', content: 'YuWanCard Wiki' }],
    ['meta', { property: 'og:image', content: OG_IMAGE }],
    ['meta', { name: 'twitter:card', content: 'summary' }],
    ['meta', { name: 'twitter:image', content: OG_IMAGE }],
  ],

  sitemap: {
    hostname: SITE_URL
  },

  lastUpdated: true,

  themeConfig: sharedThemeConfig,

  locales: {
    zhs: {
      label: '简体中文',
      lang: 'zh-CN',
      link: '/zhs/',
      title: 'YuWanCard 文档',
      description: 'YuWanCard 杀戮尖塔2 猪角色模组文档',
      themeConfig: {
        nav: [
          { text: '首页', link: '/zhs/' },
          { text: '卡牌', link: '/zhs/cards/' },
          { text: '遗物', link: '/zhs/relics/' },
          { text: '能力', link: '/zhs/powers/' }
        ],
        sidebar: [
          {
            text: '文档',
            items: [
              { text: '卡牌', link: '/zhs/cards/' },
              { text: '遗物', link: '/zhs/relics/' },
              { text: '能力', link: '/zhs/powers/' },
              { text: '附魔', link: '/zhs/enchantments/' },
              { text: '充能球', link: '/zhs/orbs/' },
              { text: '怪物', link: '/zhs/monsters/' },
              { text: '事件', link: '/zhs/events/' },
              { text: '先古之民', link: '/zhs/ancients/' },
              { text: '修改器', link: '/zhs/modifiers/' },
              { text: '角色', link: '/zhs/characters/' },
            ]
          }
        ],
        outline: { label: '本页内容', level: [2, 3] },
        docFooter: { prev: '上一页', next: '下一页' },
        darkModeSwitchLabel: '深色模式',
        sidebarMenuLabel: '菜单',
        returnToTopLabel: '回到顶部',
        langMenuLabel: '语言',
        lastUpdated: { text: '最后更新于' },
        editLink: {
          text: '在 GitHub 编辑此页',
          pattern: ''
        }
      }
    },
    eng: {
      label: 'English',
      lang: 'en-US',
      link: '/eng/',
      title: 'YuWanCard Wiki',
      description: 'YuWanCard Slay the Spire 2 Pig Character Mod Documentation',
      themeConfig: {
        nav: [
          { text: 'Home', link: '/eng/' },
          { text: 'Cards', link: '/eng/cards/' },
          { text: 'Relics', link: '/eng/relics/' },
          { text: 'Powers', link: '/eng/powers/' }
        ],
        sidebar: [
          {
            text: 'Docs',
            items: [
              { text: 'Pig Cards', link: '/eng/cards/' },
              { text: 'Colorless', link: '/eng/cards/' },
              { text: 'Tokens', link: '/eng/cards/' },
              { text: 'All Relics', link: '/eng/relics/' },
              { text: 'All Powers', link: '/eng/powers/' },
              { text: 'Enchantments', link: '/eng/enchantments/' },
              { text: 'Orbs', link: '/eng/orbs/' },
              { text: 'Monsters', link: '/eng/monsters/' },
              { text: 'Events', link: '/eng/events/' },
              { text: 'Ancients', link: '/eng/ancients/' },
              { text: 'Modifiers', link: '/eng/modifiers/' },
              { text: 'Character', link: '/eng/characters/' },
            ]
          }
        ],
        outline: { label: 'On this page', level: [2, 3] },
        lastUpdated: { text: 'Last updated' },
        editLink: {
          text: 'Edit this page on GitHub',
          pattern: ''
        }
      }
    }
  },

  base: '/Sts2-YuWanCard/',

  // Rewrite raw image/asset paths in markdown to include base prefix
  transformHtml(code) {
    return code.replace(/(src|href|content)="\/(images|assets)\//g, `$1="/Sts2-YuWanCard/$2/`)
  },

  cleanUrls: true
})
