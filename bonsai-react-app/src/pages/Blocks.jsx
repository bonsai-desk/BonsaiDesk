import React, {useEffect, useRef, useState} from 'react';
import {observer} from 'mobx-react-lite';
import {MenuContentTabbed} from '../components/MenuContent';
import {InstantButton} from '../components/Button';
import {
    grayButtonClass,
    grayButtonClassInert,
    greenButtonClass,
    lightGrayButtonClass,
    orangeButtonClass,
} from '../cssClasses';
import {useStore} from '../DataProvider';
import axios from 'axios';
import moment from 'moment';
import {InfoItemCustom} from '../components/InfoItem';
import BlockImg from '../static/block-line.svg';
import ThumbImg from '../static/thumb-up.svg';
import MenuImg from '../static/menu.svg';
import DownloadImg from '../static/download.svg';
import PostImg from '../static/post-add.svg';
import jwt from 'jsonwebtoken';
import {Route, Switch, useHistory, useRouteMatch} from 'react-router-dom';
import {postDeleteBuild, postSaveBuild, postSpawnBuild, postSpawnBuildById, postStageBuild} from '../api';
import {action} from 'mobx';
import {apiBaseManual} from '../utilities';

const DeleteState = {
    None: 0, Failed: 1, Success: 2,
};

const PublishState = {
    None: 0, Working: 1, Failed: 2, Success: 3,
};

const SaveState = {
    None: 0, Working: 1, Failed: 2, Success: 3,
};

let hamburgerButton = 'relative h-20 w-20 rounded-full cursor-pointer flex flex-wrap content-center font-bold bg-gray-800  active:bg-gray-700  hover:bg-gray-600';
let redHamburgerButton = 'relative h-20 w-20 rounded-full cursor-pointer flex flex-wrap content-center font-bold bg-red-800';

let SpawnButtonLocal = observer(({buildId}) => {
    function onClick() {
        postSpawnBuildById(buildId);
    }

    return <InstantButton onClick={onClick} className={hamburgerButton}>
        <img src={DownloadImg} alt={'Menu'} className={'absolute left-5 bottom-5 w-10'}/>
    </InstantButton>;
});

let SpawnButton = observer(({build_id}) => {
    let [failed, setFailed] = useState(false);
    let {store} = useStore();
    let url = store.ApiBase + `/blocks/builds/${build_id}`;

    function fetchBuildData() {
        if (failed) {
            return;
        }
        axios.get(url).then(resp => {
            postSpawnBuild(resp.data.data);
        }).catch(err => {
            console.log(err);
            setFailed(true);
        });
    }

    let className = failed ? redHamburgerButton : hamburgerButton;

    return <InstantButton onClick={fetchBuildData} className={className}>
        <img src={DownloadImg} alt={'Menu'} className={'absolute left-5 bottom-5 w-10'}/>
    </InstantButton>;
});

let ThumbButton = observer((props) => {
    let {store} = useStore();
    let {imgSrc, className, likes, buildId} = props;
    let token = store.BonsaiToken;
    let [liked, setLiked] = useState(props.liked);

    likes = parseInt(likes);

    function postLike() {
        let url = store.ApiBase + '/blocks/like';
        if (!props.liked) {
            setLiked(true);
            axios({
                method: 'POST',
                url: url,
                data: `token=${token}&build_id=${buildId}`,
                headers: {'content-type': 'application/x-www-form-urlencoded'},
            }).then(_ => {
                //console.log(response);
            }).catch(err => {
                console.log(err);
                setLiked(false);
            });
        }
    }

    let buttonClass;
    if (className) {
        buttonClass = className;
    } else {
        buttonClass = 'z-0 bg-gray-800 active:bg-gray-700 hover:bg-gray-600 rounded-full cursor-pointer w-20 h-20 flex flex-wrap content-center';
    }

    var _likes = likes;

    if (liked) {
        buttonClass = 'z-0 bg-bonsai-green rounded-full cursor-pointer w-20 h-20 flex flex-wrap content-center';
        if (!props.liked) {
            _likes = likes + 1;
        }
    }

    return (
            <InstantButton className={buttonClass} onClick={postLike}>
                <div className={'relative w-full flex justify-center'}>
                    <img className={'h-10 w-10 absolute -bottom-2 left-5 z-0'}
                         src={imgSrc} alt={''}/>
                    <div className={'absolute -bottom-9 left-5'}>
                        <div className={'w-10 flex flex-wrap justify-center'}>
                            {_likes}
                        </div>
                    </div>
                </div>
            </InstantButton>
    );
});

let LocalBlockPost = observer(({Name, Id}) => {
    let [modal, setModal] = useState(false);
    let [deleteModal, setDeleteModal] = useState(false);
    let [publishState, setPublishState] = useState(PublishState.None);
    let [publishModal, setPublishModal] = useState(false);
    let {builds, store} = useStore();
    let history = useHistory();

    let title = Name;
    let slug = Id;

    function clickOut() {
        setModal(false);
    }

    function spawnDeleteModal() {
        setDeleteModal(true);
    }

    function spawnModal() {
        setModal(true);
    }

    function postDelete() {
        postDeleteBuild(Id);
    }

    function spawnPublishModal() {
        setPublishModal(true);
    }

    function PublishButton() {
        return <InstantButton onClick={spawnPublishModal} className={hamburgerButton}>
            <img src={PostImg} alt={'Menu'} className={'absolute left-5 bottom-5 w-10'}/>
        </InstantButton>;
    }

    function MiniPublishModal() {
        function clickOut() {
            setPublishModal(false);
            if (publishState === PublishState.Success) {
                history.push('/menu/blocks/profile');
            }
        }

        let title = 'Publish Post?';
        let info = 'Show off your creation to the world.';
        let leftButton = 'Cancel';
        let rightButton = 'Publish';

        switch (publishState) {
            case PublishState.Success:
                title = 'Done!';
                info = `You just published ${Name}.`;
                leftButton = 'Close';
                break;
            case PublishState.Failed:
                title = 'Publish Failed!';
                info = `Something went wrong.`;
                leftButton = 'Close';
                break;
            default:
                break;
        }

        let publishButtonClass = greenButtonClass;
        if (publishState !== PublishState.None) {
            publishButtonClass = grayButtonClassInert;
        }

        function postBuild(name, data) {
            let url = store.ApiBase + '/blocks/builds';
            let token = store.BonsaiToken;
            axios({
                method: 'POST',
                url: url,
                data: `name=${name}&data=${data}&token=${token}`,
                headers: {'content-type': 'application/x-www-form-urlencoded'},
            }).then(resp => {
                console.log(resp);
                setPublishState(PublishState.Success);
            }).catch(err => {
                console.log(err);
                setPublishState(PublishState.Failed);
            });
        }

        function publishBuild() {
            if (publishState !== PublishState.None) {
                return;
            }
            postStageBuild(Id);
            setPublishState(PublishState.Working);
            let query = setInterval(() => {
                if (builds.Staging.Id === Id) {
                    clearInterval(query);
                    query = null;
                    let name = builds.Staging.Name;
                    let data = builds.Staging.Data;
                    if (data.length > 0) {
                        postBuild(name, data);
                    } else {
                        setPublishState(PublishState.Failed);
                    }
                }

            }, 25);
            setTimeout(() => {
                if (query) {
                    clearInterval(query);
                    setPublishState(PublishState.Failed);
                }
            }, 250);
        }

        return <MiniModalAction title={title} info={info} clickOut={clickOut}>
            <InstantButton onClick={clickOut} className={grayButtonClass}>{leftButton}</InstantButton>
            <InstantButton onClick={publishBuild} className={publishButtonClass}>{rightButton}</InstantButton>
        </MiniModalAction>;
    }

    function MiniDeleteModal() {
        function clickOut() {
            setDeleteModal(false);
        }

        let title = 'Delete Post?';
        let info = 'Are you sure you want to delete your post? You can\'t undo this.';
        let leftButton = 'Cancel';
        let rightButton = 'Delete';

        let deleteButtonClass = orangeButtonClass;

        return <MiniModalAction title={title} info={info} clickOut={clickOut}>
            <InstantButton onClick={clickOut} className={grayButtonClass}>{leftButton}</InstantButton>
            <InstantButton onClick={postDelete} className={deleteButtonClass}>{rightButton}</InstantButton>
        </MiniModalAction>;
    }

    function BuildModal() {
        return <Modal clickOut={clickOut}>
            <div className={'flex flex-wrap p-20 content-between h-full'}>
                <div className={''}>
                    <div className={'text-xl'}>{title}</div>
                    <div className={'text-gray-400'}>{slug}</div>
                </div>
                <div className={'flex w-full justify-end space-x-4'}>
                    <InstantButton onClick={() => {
                        setModal(false);
                    }} className={grayButtonClass}>Close</InstantButton>
                    <InstantButton onClick={spawnDeleteModal} className={orangeButtonClass}>Delete</InstantButton>
                </div>
            </div>
        </Modal>;
    }

    return <React.Fragment><InfoItemCustom title={title} imgSrc={BlockImg}
                                           leftItems={''}>

        {modal ?
                <BuildModal/> : ''
        }
        {deleteModal ? <MiniDeleteModal/> : ''}
        {publishModal ? <MiniPublishModal/> : ''}
        <div className={'flex flex-wrap content-center space-x-4'}>
            <PublishButton/>
            <SpawnButtonLocal buildId={Id}/>
            <InstantButton onClick={spawnModal} className={hamburgerButton}>
                <img src={MenuImg} alt={'Menu'} className={'absolute left-5 bottom-5 w-10'}/>
            </InstantButton>
        </div>
    </InfoItemCustom>
    </React.Fragment>;
});

let BlockPost = observer(({
                              build_name, user_name, created_at, likes, build_id, liked, user_id,
                          }) => {
    let {store} = useStore();
    let [reported, setReported] = useState(false);
    let [modal, setModal] = useState(false);
    let [deleteModal, setDeleteModal] = useState(false);
    let [deleteState, setDeleteState] = useState(DeleteState.None);
    const ReportState = {
        None: 0, Failed: 1, Success: 2,
    };
    let [reportState, setReportState] = useState(ReportState.None);
    let [reportModal, setReportModal] = useState(false);
    let decoded = jwt.decode(store.BonsaiToken);

    let myPost = false;
    if (decoded) {
        myPost = decoded.user_id === user_id;
    }

    const ago = moment(created_at).fromNow();
    const title = build_name;
    const slug = `${myPost ? 'You' : user_name} ${ago}`;
    const LeftItems = <div className={'flex space-x-4 flex-wrap'}>
        <ThumbButton imgSrc={ThumbImg} likes={likes} buildId={build_id} liked={liked}/>
        <SpawnButton build_id={build_id}/>
        <InstantButton onClick={handleClickBurger} className={hamburgerButton}>
            <img src={MenuImg} alt={'Menu'} className={'absolute left-5 bottom-5 w-10'}/>
        </InstantButton>
    </div>;

    if (deleteState === DeleteState.Success && !deleteModal) {
        return '';
    }

    function postReport() {
        let url = store.ApiBase + '/blocks/report';
        axios({
            method: 'POST',
            url: url,
            data: `token=${store.BonsaiToken}&build_id=${build_id}`,
            headers: {'content-type': 'application/x-www-form-urlencoded'},
        }).then(response => {
            if (response.data.done === 0 || response.data.done === 1) {
                setReportState(ReportState.Success);
                setReported(true);
            }
        }).catch(err => {
            setReportState(ReportState.Failed);
            console.log(err);
        });
    }

    function spawnDeleteModal() {
        setDeleteModal(true);
    }

    function spawnReportModal() {
        setReportModal(true);
    }

    function clickOut() {
        setModal(false);
    }

    function SpawnReportButton() {
        if (reported) {
            return <InstantButton onClick={() => {
            }} className={orangeButtonClass}>Reported!</InstantButton>;
        }
        return <InstantButton onClick={spawnReportModal} className={orangeButtonClass}>Report</InstantButton>;

    }

    function BuildModal() {
        return <Modal clickOut={clickOut}>
            <div className={'flex flex-wrap p-20 content-between h-full'}>
                <div className={''}>
                    <div className={'text-xl'}>{title}</div>
                    <div className={'text-gray-400'}>{slug}</div>
                </div>
                <div className={'flex w-full justify-end space-x-4'}>
                    <InstantButton onClick={() => {
                        setModal(false);
                    }} className={grayButtonClass}>Close</InstantButton>
                    {!myPost ?

                            <SpawnReportButton/>
                            : ''
                    }
                    {myPost ?
                            <InstantButton onClick={spawnDeleteModal}
                                           className={orangeButtonClass}>Delete</InstantButton>
                            : ''
                    }
                </div>
            </div>
        </Modal>;
    }

    function postDelete() {
        if (deleteState !== DeleteState.None) {
            return;
        }
        let url = store.ApiBase + `/blocks/builds/${build_id}`;
        axios({
            method: 'DELETE',
            url: url,
            data: `token=${store.BonsaiToken}`,
            headers: {'content-type': 'application/x-www-form-urlencoded'},
        }).then(response => {
            if (response.status === 200) {
                setDeleteState(DeleteState.Success);
                setModal(false);
            } else {
                setDeleteState(DeleteState.Failed);
            }
        }).catch(console.log);
    }

    function MiniDeleteModal() {
        function clickOut() {
            setDeleteModal(false);
        }

        let title = 'Delete Post?';
        let info = 'Are you sure you want to delete your post? You can\'t undo this.';
        let leftButton = 'Cancel';
        let rightButton = 'Delete';

        switch (deleteState) {
            case DeleteState.Success:
                title = 'Delete Confirmed!';
                info = `You just deleted ${build_name}.`;
                leftButton = 'Close';
                break;
            case DeleteState.Failed:
                title = 'Delete Failed!';
                info = `Something went wrong.`;
                leftButton = 'Close';
                break;
            default:
                break;
        }

        let deleteButtonClass = orangeButtonClass;
        if (deleteState !== DeleteState.None) {
            deleteButtonClass = grayButtonClassInert;
        }

        return <MiniModalAction title={title} info={info} clickOut={clickOut}>
            <InstantButton onClick={clickOut} className={grayButtonClass}>{leftButton}</InstantButton>
            <InstantButton onClick={postDelete} className={deleteButtonClass}>{rightButton}</InstantButton>
        </MiniModalAction>;
    }

    function MiniReportModal() {
        function clickOut() {
            setReportModal(false);
        }

        let title = 'Report Post?';
        let info = 'Is there something offensive about this? Let us know.';
        let leftButton = 'Cancel';
        let rightButton = 'Report';

        switch (reportState) {
            case ReportState.Success:
                title = 'Post Reported!';
                info = 'We will look into it.';
                leftButton = 'Close';
                break;
            case ReportState.Failed:
                title = 'Failed to Report';
                info = 'Something went wrong.';
                leftButton = 'Close';
                break;
            default:
                break;
        }

        function CancelButton() {
            return <InstantButton onClick={clickOut} className={grayButtonClass}>{leftButton}</InstantButton>;
        }

        function ReportButton() {
            function onClick() {
                if (reportState === ReportState.None) {
                    postReport();
                }
            }

            let className = orangeButtonClass;
            if (reportState !== ReportState.None) {
                className = grayButtonClassInert;
            }
            return <InstantButton onClick={onClick} className={className}>{rightButton}</InstantButton>;
        }

        return <MiniModalAction title={title} info={info} clickOut={clickOut}>
            <CancelButton/>
            <ReportButton/>
        </MiniModalAction>;
    }

    function handleClickBurger() {
        setModal(true);
    }

    return <React.Fragment><InfoItemCustom title={title} slug={slug} imgSrc={BlockImg}
                                           leftItems={''}>
        {deleteModal ? <MiniDeleteModal/> : ''}
        {reportModal ? <MiniReportModal/> : ''}

        {modal ?
                <BuildModal/> : ''
        }
        <div className={'flex flex-wrap content-center space-x-4'}>
            {LeftItems}
        </div>
    </InfoItemCustom>
    </React.Fragment>;
});

function MiniModalAction({clickOut, title, info, children}) {
    return <MiniModal clickOut={clickOut}>
        <div className={''}>
            <div className={''}>
                <div className={'text-xl px-6 py-6'}>{title}</div>
                <div className={'px-6 py-8'}>{info}</div>
            </div>
            <div className={'py-4 px-4 w-full flex flex-wrap space-x-4 justify-end px-2'}>
                {children}
            </div>
        </div>
    </MiniModal>;
}

const NewPage = observer(() => {
    let [data, setData] = useState([]);
    let {store} = useStore();

    let url = store.ApiBase + `/blocks/new?token=${store.BonsaiToken}`;

    useEffect(() => {
        axios.get(url).then(response => {
            setData(response.data);
        }).catch(console.log);

    }, [url]);

    return <React.Fragment>
        <Spacer/>
        {data.map(x => <BlockPost key={x.build_name + x.created_at} {...x}/>)}
    </React.Fragment>;
});

const HotPage = observer(() => {
    let [data, setData] = useState([]);
    let {store} = useStore();

    let url = store.ApiBase + `/blocks/hot?token=${store.BonsaiToken}`;

    useEffect(() => {
        axios.get(url).then(response => {
            setData(response.data);
        }).catch(console.log);

    }, [url]);

    return <React.Fragment>
        <Spacer/>
        {data.map(x => <BlockPost key={x.build_name + x.created_at} {...x}/>)}
    </React.Fragment>;
});

let ProdPage = observer(() => {
    let [data, setData] = useState([]);
    let url = apiBaseManual('PRODUCTION') + '/blocks/debug';

    useEffect(() => {
        axios.get(url).then(response => {
            setData(response.data);
        }).catch(console.log);

    }, [url]);
    return <React.Fragment>
        <Spacer/>
        {data.map(x => <BlockPost key={x.build_name + x.created_at} {...x}/>)}
    </React.Fragment>;
});

function Modal({children, clickOut}) {
    const parentEl = useRef(null);

    function onPointerDown(e) {
        if (parentEl.current === e.target) {
            clickOut();
        }
    }

    return <div ref={parentEl} onPointerDown={onPointerDown}
                className={'bg-opacity-90 z-20 absolute top-0 left-0 w-screen h-screen bg-gray-900 flex flex-wrap content-center justify-center'}>
        <div className={'border-4 z-30 h-3/4 w-3/4 rounded-3xl bg-gray-900 overflow-hidden'}>
            {children}
        </div>
    </div>;
}

function MiniModal({children, clickOut}) {
    const parentEl = useRef(null);

    function onPointerDown(e) {
        if (parentEl.current === e.target) {
            clickOut();
        }
    }

    return <div ref={parentEl} onPointerDown={onPointerDown}
                className={'bg-opacity-90 z-40 absolute top-0 left-0 w-screen h-screen bg-gray-900 flex flex-wrap content-center justify-center'}>
        <div className={'border-4 z-50 rounded-xl bg-gray-900 overflow-hidden w-7/12'}>
            {children}
        </div>
    </div>;
}

const DraftsPage = observer((props) => {
    const searchParams = new URLSearchParams(props.location.search);
    let modalOn = searchParams.get('modal');
    let [modal, setModal] = useState(!!modalOn);
    let {builds} = useStore();
    let input = useRef();
    let [saveState, setSaveState] = useState(SaveState.None);
    let [savedBuildName, setSavedBuildName] = useState('');
    let history = useHistory();
    let [emptyString, setEmptyString] = useState(false);

    let data = builds.List;

    useEffect(() => {
        setTimeout(() => {
            if (data.length === 0) {
                setEmptyString(true);
            }
        }, 250);
    });

    let setSavedNotOk = action(() => {
        builds.SavedOk = false;
    });

    function saveBuild() {
        if (saveState !== SaveState.None) {
            return;
        }
        if (input.current) {
            if (input.current.value.length > 0) {
                setSavedBuildName(input.current.value);
                setSavedNotOk();
                setSaveState(SaveState.Working);
                let query = setInterval(() => {
                    console.log(builds.SavedOk);
                    if (builds.SavedOk) {
                        clearInterval(query);
                        query = null;
                        setSaveState(SaveState.Success);
                    }
                }, 100);
                setTimeout(() => {
                    if (query) {
                        setSaveState(SaveState.Failed);
                        clearInterval(query);
                    }
                }, 500);

                postSaveBuild(input.current.value);
            }
        }
    }

    let buttonClass = saveState === SaveState.None ? greenButtonClass : grayButtonClassInert;

    function NewBuildModal() {
        let [initialClear, setInitialClear] = useState(false);

        function onClick() {
            if (!initialClear) {
                if (input.current) {
                    input.current.value = '';
                    setInitialClear(true);
                }
            }
        }

        if (saveState === SaveState.None || saveState === SaveState.Working) {
            return <Modal clickOut={() => {
            }}>
                <div className={'px-20 py-20 h-full flex flex-wrap content-between'}>
                    <div className={'w-full text-3xl'}>
                        Your Build
                    </div>
                    <input spellCheck={false} className={'w-full p-2 text-gray-100 bg-gray-700 rounded text-3xl'}
                           onClick={onClick}
                           ref={input}
                           placeholder={'Title'} type={'text'} defaultValue={'New Build'}/>
                    <div className={'flex flex-wrap justify-end space-x-4 w-full'}>
                        <InstantButton onClick={() => {
                            setModal(false);
                            setSaveState(SaveState.None);
                            input.current.value = '';
                            setSavedBuildName('');
                            history.push('/menu/blocks/drafts');
                        }} className={grayButtonClass}>Cancel</InstantButton>
                        <InstantButton onClick={saveBuild} className={buttonClass}>Save</InstantButton>
                    </div>
                </div>
            </Modal>;
        }

        let title = savedBuildName;
        let info = 'Your build has been saved locally!';

        if (saveState === SaveState.Failed) {
            title = 'Failed to save build!';
            info = 'Something went wrong.';
        }

        return <Modal clickOut={() => {
        }}>
            <div className={'px-20 py-20 h-full flex flex-wrap content-between'}>
                <div className={'w-full text-3xl'}>
                    {title}
                </div>
                <div>
                    {info}
                </div>
                <div className={'flex flex-wrap justify-end space-x-4 w-full'}>
                    <InstantButton onClick={() => {
                        setModal(false);
                        history.push('/menu/blocks/drafts');
                    }} className={grayButtonClass}>Close</InstantButton>
                </div>
            </div>
        </Modal>;

    }

    function EmptyMessage() {
        return <div className={'flex flex-wrap w-full justify-center h-full content-center'}>
            Nothing Here.
        </div>;
    }

    return <React.Fragment>
        {modal ? <NewBuildModal/> : ''}
        <Spacer/>
        {emptyString ? <EmptyMessage/> : ''}
        {data.map(x => {
            return <LocalBlockPost key={x.Id} {...x}/>;
        })}
    </React.Fragment>;

});

function Spacer() {
    return <div className={'mt-2 rounded h-4 bg-gray-700'}/>;
}

function Header(props) {
    return <div className={'mt-2 rounded px-4 h-16 bg-gray-700 text-3xl flex flex-wrap content-center justify-between'}>
        {props.children}
    </div>;

}

function UserInfo({display_name, likes}) {

    likes = likes ? 'You have ' + likes + ' Likes' : '';
    return <Header>
        <div>{display_name}</div>
        <div>{likes}</div>
    </Header>;
}

const ProfilePage = observer(() => {
    let [data, setData] = useState([]);
    let [userData, setUserData] = useState({});
    let {store} = useStore();
    let [emptyString, setEmptyString] = useState(false);
    let [timerDone, setTimerDone] = useState(false);

    useEffect(() => {
        setTimeout(() => {
            setTimerDone(true);
        }, 250);

    }, []);

    useEffect(() => {
        if (timerDone && data.length === 0) {
            setEmptyString(true);
        } else if (timerDone && data.length > 0) {
            setEmptyString(false);
        }
    }, [timerDone, data]);

    let decoded = jwt.decode(store.BonsaiToken);
    let profile_url = store.ApiBase + `/blocks/users/${decoded.user_id}/info`;

    useEffect(() => {
        axios.get(profile_url).then(response => {
            setUserData(response.data);
        }).catch(console.log);

    }, [profile_url]);

    let url = store.ApiBase + `/blocks/users/${decoded.user_id}`;

    useEffect(() => {
        axios.get(url).then(response => {
            setData(response.data);
        }).catch(console.log);
    }, [url]);

    function EmptyMessage() {
        return <div className={'flex flex-wrap w-full justify-center'}>
            Publish builds from your saved tab.
        </div>;
    }

    return <React.Fragment>
        <UserInfo {...userData}/>
        {data.map(x => <BlockPost key={x.build_name + x.created_at} {...x}/>)}
        {emptyString ? <EmptyMessage/> : ''}
    </React.Fragment>;
});

export const BlocksPage = observer(() => {
    let match = useRouteMatch();
    let history = useHistory();

    let {store} = useStore();
    let dev = store.AppInfo.Build === 'DEVELOPMENT';

    let path = window.location.pathname.split('/');
    let loc = path[path.length - 1];

    let hotButtonClass = loc === 'hot' ? lightGrayButtonClass : grayButtonClass;
    let newButtonClass = loc === 'new' ? lightGrayButtonClass : grayButtonClass;
    let profileButtonClass = loc === 'profile' ? lightGrayButtonClass : grayButtonClass;
    let draftsButtonClass = loc === 'drafts' ? lightGrayButtonClass : grayButtonClass;
    let prodButtonClass = loc === 'prod' ? lightGrayButtonClass : grayButtonClass;

    function goHot() {
        history.push(match.path + '/hot');
    }

    function goNew() {
        history.push(match.path + '/new');
    }

    function goProfile() {
        history.push(match.path + '/profile');
    }

    function goDrafts() {
        history.push(match.path + '/drafts');
    }

    function goProd() {
        history.push(match.path + '/prod');
    }

    let navBar = <div className={'flex flex-wrap w-full space-x-14 justify-center'}>
        <InstantButton className={hotButtonClass} onClick={goHot}>Top</InstantButton>
        {dev ? <InstantButton className={prodButtonClass} onClick={goProd}>Prod</InstantButton> : ''}
        <InstantButton className={newButtonClass} onClick={goNew}>New</InstantButton>
        <InstantButton className={profileButtonClass} onClick={goProfile}>Published</InstantButton>
        <InstantButton className={draftsButtonClass} onClick={goDrafts}>Saved</InstantButton>
    </div>;

    return <MenuContentTabbed name={'Blocks'} navBar={navBar}>
        <Switch>
            <Route path={`${match.path}/hot`} component={HotPage}/>
            <Route path={`${match.path}/prod`} component={ProdPage}/>
            <Route path={`${match.path}/new`} component={NewPage}/>
            <Route path={`${match.path}/profile`} component={ProfilePage}/>
            <Route path={`${match.path}/drafts`} component={DraftsPage}/>
        </Switch>
    </MenuContentTabbed>;
});
