<template>
  <QuizLayout>
    <section class="welcome-panel">
      <div class="welcome-text"><span class="eyebrow">HỌC TẬP TRỰC TUYẾN · LỚP 5–12</span><h1>Học tốt mỗi ngày,<br /><em>tự tin mỗi bài thi!</em></h1><p>Kho bài học và câu hỏi được thiết kế để em luyện tập theo cách của riêng mình.</p><a class="hero-cta" href="#chon-mon">Khám phá môn học <b>→</b></a><div class="welcome-pills"><span>📚 Bài giảng dễ hiểu</span><span>📝 Luyện tập chủ động</span><span>🗼 Chinh phục từng tầng</span></div></div>
      <div class="welcome-visual" aria-hidden="true"><div class="visual-circle"></div><div class="visual-book"><StudyIllustration /></div><span class="visual-star star-a">✦</span><span class="visual-star star-b">✧</span><div class="visual-label">Cùng học<br />cùng tiến bộ!</div></div>
    </section>

    <section class="catalog-section" id="chon-mon">
      <div class="section-heading"><div><span class="eyebrow">BẮT ĐẦU HÀNH TRÌNH</span><h2>Chọn môn học của em</h2><p>Chọn khối lớp rồi vào không gian học tập riêng của môn đó.</p></div><label class="grade-picker">Khối lớp<select v-model.number="grade" aria-label="Chọn khối lớp"><option v-for="item in grades" :key="item" :value="item">Lớp {{ item }}</option></select></label></div>
      <div class="subject-grid">
        <RouterLink v-for="subject in subjects" :key="subject.slug" :to="{ path: `/subjects/${subject.slug}`, query: { grade } }" class="subject-card" :class="subject.kind" :data-subject="subject.slug">
          <div class="card-top"><span class="subject-symbol">{{ subject.icon }}</span><span class="class-label">LỚP {{ grade }}</span></div><h3>{{ subject.name }}</h3><p>{{ subject.description }}</p><div class="card-features"><span>📚 Bài học</span><span>📝 Luyện tập</span></div><span class="choose-link">Vào môn học <b>→</b></span>
        </RouterLink>
      </div>
    </section>


  </QuizLayout>
</template>

<script setup>
import { ref } from 'vue'
import { RouterLink } from 'vue-router'
import QuizLayout from './QuizLayout.vue'
import StudyIllustration from './ui/StudyIllustration.vue'

const grades = Array.from({ length: 8 }, (_, index) => index + 5)
const grade = ref(5)
const subjects = [
  { slug: 'toan', name: 'Toán', icon: '🔢', kind: 'math', description: 'Rèn tư duy logic, khám phá con số và giải những bài toán thú vị.' },
  { slug: 'tieng-anh', name: 'Tiếng Anh', icon: '🌍', kind: 'english', description: 'Luyện từ vựng, ngữ pháp và kỹ năng tiếng Anh mỗi ngày.' },
  { slug: 'tieng-viet', name: 'Tiếng Việt', icon: '📖', kind: 'vietnamese', description: 'Khám phá câu chữ, đọc hiểu và vẻ đẹp của tiếng Việt.' }
]
</script>

<style scoped>
.welcome-panel{position:relative;isolation:isolate;display:flex;align-items:center;min-height:390px;overflow:hidden;padding:46px 5%;border:1px solid var(--subject-line);border-radius:28px;background:radial-gradient(ellipse at 90% 20%,var(--subject-tint),transparent 52%),linear-gradient(115deg,#fff,var(--subject-soft));color:var(--subject-ink)}
.welcome-text{position:relative;z-index:1;max-width:640px}.eyebrow{display:inline-block;color:var(--subject-primary);font-size:11px;font-weight:750;letter-spacing:.12em;line-height:1.6}.welcome-panel h1{margin:17px 0;font-size:clamp(34px,4.1vw,49px);font-weight:750;line-height:1.17;letter-spacing:-.045em}.welcome-panel h1 em{color:var(--subject-strong);font-style:normal}.welcome-panel p{max-width:470px;margin:0;color:var(--subject-muted);font-size:15px;line-height:1.8}
.hero-cta{display:inline-flex;align-items:center;justify-content:center;gap:22px;min-height:46px;margin-top:25px;padding:12px 20px;border:1px solid var(--subject-strong);border-radius:12px;background:var(--subject-strong);box-shadow:0 5px 12px color-mix(in srgb,var(--subject-primary) 8%,transparent);color:#fff;text-decoration:none;font-size:13px;font-weight:750;transition:transform .18s,box-shadow .18s}.hero-cta:hover{transform:translateY(-2px);box-shadow:0 8px 18px color-mix(in srgb,var(--subject-primary) 14%,transparent)}.hero-cta .learning-icon{font-size:18px}
.welcome-pills{display:flex;flex-wrap:wrap;gap:12px 20px;margin-top:22px}.welcome-pills span{display:inline-flex;align-items:center;gap:7px;color:var(--subject-muted);font-size:11px;line-height:1.7}.welcome-pills .learning-icon{color:var(--subject-primary);font-size:16px}
.welcome-visual{position:absolute;z-index:0;right:5%;top:50%;width:320px;height:295px;transform:translateY(-50%);display:grid;place-items:center}.visual-circle{position:absolute;width:265px;height:265px;border:1px solid #ffffffc9;border-radius:50%;background:linear-gradient(145deg,#ffffffa8,color-mix(in srgb,var(--subject-tint) 47%,transparent))}.visual-book{position:relative;width:300px;filter:drop-shadow(0 12px 14px #8054b00c);animation:art-float 6s ease-in-out infinite}.visual-star{position:absolute;color:color-mix(in srgb,var(--subject-primary) 45%,#fff);font-size:26px}.star-a{top:8px;right:30px}.star-b{bottom:20px;left:8px}.visual-label{position:absolute;right:0;bottom:12px;padding:12px 17px;border:1px solid #fff;border-radius:13px;background:#fffffff0;color:var(--subject-strong);text-align:center;font-size:12px;line-height:1.5;font-weight:650;transform:rotate(5deg);box-shadow:0 6px 18px #8054b009}
.catalog-section{padding-top:42px;scroll-margin-top:100px}.section-heading{display:flex;align-items:end;justify-content:space-between;gap:20px;margin-bottom:22px}.section-heading h2{margin:7px 0;color:var(--subject-ink);font-size:27px;font-weight:750;letter-spacing:-.035em}.section-heading p{margin:0;color:var(--subject-muted);font-size:14px;line-height:1.7}.grade-picker{display:flex;align-items:center;gap:12px;padding:10px 13px;border:1px solid var(--subject-line);border-radius:12px;background:#fff;color:var(--subject-muted);font-size:12px;font-weight:600}.grade-picker select{border:0;background:transparent;color:var(--subject-ink);font:inherit;font-weight:750;cursor:pointer}.grade-picker:focus-within{outline:3px solid var(--subject-tint);outline-offset:3px}
.subject-grid{display:grid;grid-template-columns:repeat(3,minmax(0,1fr));gap:20px}.subject-card{--subject-accent:var(--subject-primary);position:relative;display:flex;flex-direction:column;min-width:0;min-height:318px;padding:26px;border:1px solid var(--subject-line);border-radius:22px;background:linear-gradient(145deg,#fff 18%,var(--subject-soft));color:var(--subject-ink);text-decoration:none;box-shadow:0 3px 10px #30254f03;transition:transform .22s,border-color .22s,box-shadow .22s,background .22s;isolation:isolate;overflow:hidden}.subject-card:hover{transform:translateY(-4px);border-color:var(--subject-primary);box-shadow:0 14px 28px color-mix(in srgb,var(--subject-primary) 10%,transparent)}.card-top{display:flex;justify-content:space-between;align-items:center}.subject-symbol{display:grid;place-items:center;width:62px;height:62px;border:1px solid var(--subject-line);border-radius:19px;background:linear-gradient(145deg,#fff,var(--subject-tint));color:var(--subject-strong);font-size:31px;box-shadow:0 7px 18px color-mix(in srgb,var(--subject-primary) 13%,transparent);transition:transform .2s,box-shadow .2s}.class-label{padding:6px 9px;border-radius:8px;background:var(--subject-soft);color:var(--subject-muted);font-size:10px;font-weight:750;letter-spacing:.05em}.subject-card h3{margin:22px 0 9px;color:var(--subject-ink);font-size:25px;font-weight:750;letter-spacing:-.035em}.subject-card p{min-height:48px;margin:0;color:var(--subject-muted);font-size:13px;line-height:1.8}.card-features{display:flex;flex-wrap:wrap;gap:14px;margin:20px 0;color:var(--subject-muted);font-size:11px}.card-features span{display:inline-flex;align-items:center;gap:6px}.card-features .learning-icon{font-size:15px;color:var(--subject-primary)}.choose-link{display:flex;justify-content:space-between;align-items:center;min-height:42px;margin-top:auto;padding-top:14px;border-top:1px solid var(--subject-line);color:var(--subject-strong);font-size:13px;font-weight:750;padding:10px 12px;border:1px solid var(--subject-line);border-radius:11px;background:color-mix(in srgb,var(--subject-tint) 55%,#fff);transition:background .18s,color .18s}.choose-link .learning-icon{font-size:19px;transition:transform .18s}.subject-card:hover .choose-link .learning-icon{transform:translateX(4px)}
a:focus-visible,button:focus-visible{outline:3px solid var(--subject-primary);outline-offset:4px}
@keyframes art-float{50%{transform:translateY(-6px)}}
@media(max-width:1000px){.welcome-visual{right:1%;width:270px;opacity:.8}.welcome-text{max-width:580px}.welcome-panel p{max-width:410px}.welcome-panel h1{font-size:42px}.subject-card{padding:22px;isolation:isolate;overflow:hidden;background:linear-gradient(145deg,#fff 18%,var(--subject-soft));transition:transform .22s,border-color .22s,box-shadow .22s,background .22s}}
@media(max-width:760px){.welcome-panel{min-height:0;padding:30px 25px 27px;border-radius:22px}.welcome-text{max-width:none;width:100%}.welcome-panel h1{font-size:clamp(32px,6vw,40px);max-width:85%}.welcome-panel p{max-width:85%;font-size:14px}.welcome-visual{right:-120px;top:120px;opacity:.22;pointer-events:none}.visual-label,.visual-star{display:none}.welcome-pills{gap:10px 15px;margin-top:23px}.welcome-pills span{font-size:11px}.hero-cta{margin-top:22px;min-height:46px}.catalog-section{padding-top:30px}.section-heading{align-items:flex-start;flex-direction:column;gap:17px}.section-heading h2{font-size:25px}.section-heading p{font-size:13px}.grade-picker{width:100%;justify-content:space-between}.subject-grid{grid-template-columns:1fr;gap:14px}.subject-card{min-height:0;padding:23px;isolation:isolate;overflow:hidden;background:linear-gradient(145deg,#fff 18%,var(--subject-soft));transition:transform .22s,border-color .22s,box-shadow .22s,background .22s}.subject-card h3{margin-top:18px}.subject-card p{min-height:0}.card-features{margin:17px 0}.choose-link{padding-top:15px;padding:10px 12px;border:1px solid var(--subject-line);border-radius:11px;background:color-mix(in srgb,var(--subject-tint) 55%,#fff);transition:background .18s,color .18s}}
@media(max-width:480px){.welcome-panel h1,.welcome-panel p{max-width:100%}.welcome-visual{display:none}.welcome-panel h1{font-size:32px}.welcome-pills{gap:10px 16px}}
@media(prefers-reduced-motion:reduce){.visual-book{animation:none}.hero-cta,.subject-card,.choose-link .learning-icon{transition:none}.hero-cta:hover,.subject-card:hover{transform:none}}
.subject-card:before{content:"";position:absolute;z-index:-1;top:-63px;right:-43px;width:190px;height:190px;border:1px solid color-mix(in srgb,var(--subject-primary) 16%,transparent);border-radius:50%;background:color-mix(in srgb,var(--subject-tint) 28%,transparent);box-shadow:0 0 0 20px color-mix(in srgb,var(--subject-tint) 15%,transparent),0 0 0 48px color-mix(in srgb,var(--subject-tint) 9%,transparent);transition:transform .25s;pointer-events:none}
.subject-card:after{content:"";position:absolute;right:22px;bottom:0;left:22px;height:3px;border-radius:5px 5px 0 0;background:linear-gradient(90deg,var(--subject-primary),var(--subject-tint));transform:scaleX(.18);transform-origin:left;transition:transform .22s;pointer-events:none}
.subject-card:hover,.subject-card:focus-visible{transform:translateY(-5px);border-color:var(--subject-primary);box-shadow:0 18px 36px color-mix(in srgb,var(--subject-primary) 16%,transparent)}
.subject-card:hover:before,.subject-card:focus-visible:before{transform:scale(1.12) translate(-5px,5px)}
.subject-card:hover:after,.subject-card:focus-visible:after{transform:scaleX(1)}
.subject-card:hover .subject-symbol,.subject-card:focus-visible .subject-symbol{transform:translateY(-2px) rotate(-4deg);box-shadow:0 11px 22px color-mix(in srgb,var(--subject-primary) 20%,transparent)}
.choose-link b{font-size:19px;transition:transform .18s}
.subject-card:hover .choose-link,.subject-card:focus-visible .choose-link{background:var(--subject-strong);color:#fff;border-color:var(--subject-strong)}
.subject-card:hover .choose-link b{transform:translateX(3px)}
.subject-card:focus-visible{outline:3px solid var(--subject-primary);outline-offset:4px}
@media(prefers-reduced-motion:reduce){.subject-card:hover,.subject-card:focus-visible,.subject-card:hover .subject-symbol,.subject-card:focus-visible .subject-symbol,.subject-card:hover:before,.subject-card:focus-visible:before{transform:none}}

.subject-card:is(:hover,:focus-visible){background:linear-gradient(135deg,color-mix(in srgb,var(--subject-primary) 13%,#fff),var(--subject-tint));border-color:var(--subject-primary);box-shadow:0 16px 32px color-mix(in srgb,var(--subject-primary) 22%,transparent),inset 0 0 0 1px var(--subject-primary)}
.subject-card:is(:hover,:focus-visible) .subject-symbol{background:var(--subject-tint);border-color:var(--subject-primary)}
.subject-card:is(:hover,:focus-visible) .choose-link{background:var(--subject-strong);border-color:var(--subject-strong);color:#fff;box-shadow:0 5px 14px color-mix(in srgb,var(--subject-primary) 22%,transparent)}
</style>
