/* Awaken UI kit — app root: full flow Splash → Plans → Onboarding → main tabs. */
const { Splash, Plans, Onboarding } = window;
const { Home, Profile, Workout, LevelUp, todayQuests } = window;
const { PhoneFrame, TabBar } = window;

function AwakenApp() {
  const [screen, setScreen] = React.useState('splash'); // splash|plans|onboarding|app
  const [tab, setTab] = React.useState('home');          // home|profile
  const [view, setView] = React.useState('home');        // home|profile|workout
  const [quests, setQuests] = React.useState(() => todayQuests().map((q) => ({ ...q, done: false })));
  const [levelUp, setLevelUp] = React.useState(false);

  const toggleQuest = (id) => setQuests((qs) => qs.map((q) => (q.id === id ? { ...q, done: !q.done } : q)));
  const go = (v) => { setView(v); if (v === 'home' || v === 'profile') setTab(v); };
  const changeTab = (t) => { setTab(t); setView(t); };
  const completeWorkout = () => { setQuests((qs) => qs.map((q) => ({ ...q, done: true }))); setLevelUp(true); };

  let body;
  if (screen === 'splash') body = <Splash onStart={() => setScreen('plans')} />;
  else if (screen === 'plans') body = <Plans onContinue={() => setScreen('onboarding')} />;
  else if (screen === 'onboarding') body = <Onboarding onDone={() => { setScreen('app'); go('home'); }} />;
  else {
    const screenEl =
      view === 'profile' ? <Profile /> :
      view === 'workout' ? <Workout go={go} quests={quests} completeWorkout={completeWorkout} /> :
      <Home go={go} quests={quests} toggleQuest={toggleQuest} />;
    body = (
      <React.Fragment>
        {screenEl}
        {view !== 'workout' && <TabBar active={tab} onChange={changeTab} onTrain={() => go('workout')} />}
        {levelUp && <LevelUp onClose={() => { setLevelUp(false); go('home'); }} />}
      </React.Fragment>
    );
  }

  return <PhoneFrame>{body}</PhoneFrame>;
}

ReactDOM.createRoot(document.getElementById('root')).render(<AwakenApp />);
