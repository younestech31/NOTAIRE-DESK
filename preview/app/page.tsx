const modules = [
  ["تحرير المستندات", "مساحة تحرير صفحات A4 وتنسيق عربي وفرنسي ومراجعة المستند."],
  ["الأطراف والعقارات", "ملفات منظمة للأشخاص والكيانات والأملاك مع سجل تغييرات."],
  ["القوالب والعقود", "قوالب قابلة للإصدار وحقول ومتغيرات وفقرات شرطية."],
  ["المراجعة الذكية", "كشف التناقضات والتنبيهات دون الحلول محل تقدير الموثق."]
];
export default function Home() {
  return <main className="shell">
    <header className="topbar"><div><strong>NOTAIRE DESK</strong><span>منصة العمل المكتبي للتوثيق</span></div><span className="status">معاينة تطويرية</span></header>
    <section className="hero"><p className="eyebrow">WORKSPACE · ON-PREMISES · LAN</p><h1>بيئة عمل موحدة<br/>لمكتب التوثيق</h1>
      <p className="lead">محرر مستندات ونظام متخصص لإدارة بيانات المكتب وإنشاء العقود، مع إبقاء البيانات والملفات داخل المؤسسة.</p>
      <div className="notice">واجهة استعراضية فقط. لا تدخل فيها بيانات حقيقية أو وثائق أو معلومات شخصية.</div>
    </section>
    <section className="grid">{modules.map(([title, description], i) => <article className="module" key={title}><span className="number">0{i+1}</span><h2>{title}</h2><p>{description}</p></article>)}</section>
    <footer>NOTAIRE DESK · نسخة أولية غير مخصصة للإنتاج</footer>
  </main>;
}
