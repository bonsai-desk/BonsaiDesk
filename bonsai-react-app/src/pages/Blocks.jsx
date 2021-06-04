import React, {useEffect, useState} from 'react';
import {observer} from 'mobx-react-lite';
import {MenuContentTabbed} from '../components/MenuContent';
import {InstantButton} from '../components/Button';
import {grayButtonClass, hamburgerButton, lightGrayButtonClass, redButtonClass} from '../cssClasses';
import {useStore} from '../DataProvider';
import axios from 'axios';
import moment from 'moment';
import {InfoItemCustom} from '../components/InfoItem';
import BlockImg from '../static/block-line.svg';
import ThumbImg from '../static/thumb-up.svg';
import MenuImg from '../static/menu.svg';
import jwt from 'jsonwebtoken';

const Tab = {
    Hot: 0,
    New: 1,
    Profile: 2,
};

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
            }).then(response => {
                console.log(response);
            }).catch(err => {
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
        buttonClass = 'z-0 bg-gray-700 active:bg-gray-700 hover:bg-gray-600 rounded-full cursor-pointer w-20 h-20 flex flex-wrap content-center';
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

let BlockPost = observer(({build_name, user_name, created_at, likes, build_id, liked}) => {
    let {store} = useStore();
    let [expanded, setExpanded] = useState(false);
    let [reported, setReported] = useState(false);
    const ago = moment(created_at).fromNow();
    const title = build_name;
    const slug = `By ${user_name} ${ago}`;
    const LeftItems = <div className={'flex space mr-4'}>
        <ThumbButton imgSrc={ThumbImg} likes={likes} buildId={build_id} liked={liked}/>
    </div>;

    function postReport(callback) {
        let url = store.ApiBase + '/blocks/report';
        axios({
            method: 'POST',
            url: url,
            data: `token=${store.BonsaiToken}&build_id=${build_id}`,
            headers: {'content-type': 'application/x-www-form-urlencoded'},
        }).catch(err => console.log).then(response => {
            if (response.data.done === 0 || response.data.done === 1) {
                setReported(true);
            }
        });
    }

    let reportInner = reported ? 'Reported' : 'Report';

    function Drawer() {
        return <div
                className={'w-full h-28 bg-gray-700 rounded-b-2xl flex flex-wrap content-center overflow-hidden px-4 justify-center'}>
            <div>
                <InstantButton onClick={postReport} className={redButtonClass}>{reportInner}</InstantButton>
            </div>
        </div>;
    }

    function handleClickBurger() {
        setExpanded(!expanded);
    }

    return <React.Fragment><InfoItemCustom key={user_name + created_at} title={title} slug={slug} imgSrc={BlockImg}
                                           leftItems={LeftItems}>
        <div className={'flex flex-wrap content-center space-x-4'}>
            <InstantButton className={grayButtonClass}>Spawn</InstantButton>
            <InstantButton onClick={handleClickBurger} className={hamburgerButton}>
                <img src={MenuImg} alt={'Menu'} className={'w-8'}/>
            </InstantButton>
        </div>
    </InfoItemCustom>
        {expanded ? <Drawer/> : ''}
    </React.Fragment>;
});

const NewPage = observer(() => {
    let [data, setData] = useState([]);
    let {store} = useStore();

    let url = store.ApiBase + `/blocks/new?token=${store.BonsaiToken}`;

    useEffect(() => {
        axios.get(url).catch(err => console.log).then(response => {
            setData(response.data);
        }).catch(err => console.log);

    }, [url]);

    return <React.Fragment>
        {data.map(x => <BlockPost {...x}/>)}
    </React.Fragment>;
});

const HotPage = observer(() => {
    let [data, setData] = useState([]);
    let {store} = useStore();

    let url = store.ApiBase + `/blocks/hot?token=${store.BonsaiToken}`;

    useEffect(() => {
        axios.get(url).catch(err => console.log).then(response => {
            setData(response.data);
        }).catch(err => console.log);

    }, [url]);

    return <React.Fragment>
        {data.map(x => <BlockPost {...x}/>)}
    </React.Fragment>;
});

const ProfilePage = observer(() => {
    let [data, setData] = useState([]);
    let {store} = useStore();

    let url = store.ApiBase + `/blocks/hot?token=${store.BonsaiToken}`;

    useEffect(() => {
        axios.get(url).catch(err => console.log).then(response => {
            setData(response.data);
        }).catch(err => console.log);

    }, [url]);

    let info = jwt.decode(store.BonsaiToken);

    return <React.Fragment>
        <div>
            {info.display_name}
        </div>
        {data.map(x => <BlockPost {...x}/>)}
    </React.Fragment>;
});

export const BlocksPage = observer(() => {
    let [activeTab, setActiveTab] = useState(Tab.Hot);

    function handleClickHot() {
        setActiveTab(Tab.Hot);
    }

    function handleClickNew() {
        setActiveTab(Tab.New);
    }

    function handleClickProfile() {
        setActiveTab(Tab.Profile);
    }

    let hotButtonClass = activeTab === Tab.Hot ? lightGrayButtonClass : grayButtonClass;
    let newButtonClass = activeTab === Tab.New ? lightGrayButtonClass : grayButtonClass;
    let profileButtonClass = activeTab === Tab.Profile ? lightGrayButtonClass : grayButtonClass;

    let Inner;

    switch (activeTab) {
        case Tab.Profile:
            Inner = ProfilePage;
            break;
        case Tab.Hot:
            Inner = HotPage;
            break;
        case Tab.New:
            Inner = NewPage;
            break;
    }

    let navBar = <div className={'flex flex-wrap w-full space-x-20 justify-center'}>
        <InstantButton className={hotButtonClass} onClick={handleClickHot}>Hot</InstantButton>
        <InstantButton className={newButtonClass} onClick={handleClickNew}>New</InstantButton>
        <InstantButton className={profileButtonClass} onClick={handleClickProfile}>Profile</InstantButton>
    </div>;
    return <MenuContentTabbed name={'Blocks'} navBar={navBar}><Inner/></MenuContentTabbed>;
});