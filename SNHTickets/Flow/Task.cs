﻿using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace SNHTickets.Flow
{
    public class Task
    {
        //商品ID
        public String id { get; set; }
        //商品名称
        public String goodsName { get; set; }
        //商品类型
        public String type { get; set; }
        //抢票模式代号
        public Int32 mode { get; set; }
        //抢票模式全名
        public String modeName { get; set; }
        //抢票的帐号
        public String accountUserName { get; set; }
        //抢票需要的帐号数量
        public Int32 accountsNum { get; set; }
        //总共要抢的数量
        public Int32 totalNum { get; set; }
        //延时时长
        public Int32 delayTime { get; set; }
        //帐号列表
        public List<Account> accountsList { get; set; }
        //任务状态
        public Boolean status { get; set; }

        //错误代码列表
        Dictionary<Int32, String> errorCodeList = new Dictionary<int, string>()
        {
            { 999995, "帐号被禁" },
            { 1001, "网络错误" },
            { 1000, "购买失败" },
            { 999, "未登录"},
            { 888, "购买达到上限" },
            { 3, "商品下架" },
            { 2, "库存不足" },
            { 0, "成功" }
        };

        public delegate void OrderResultEventHandler(Object sender, OrderResultEventArgs e);
        public event OrderResultEventHandler OrderResultEvent;

        public class OrderResultEventArgs : EventArgs
        {
            public readonly String account;
            public readonly Int32 errorCode;
            public readonly String errorMessage;
            public OrderResultEventArgs(String account, Int32 errorCode, String errorMessage)
            {
                this.account = account;
                this.errorCode = errorCode;
                this.errorMessage = errorMessage;
            }
        }

        protected virtual void DispatchOrderCompleteEvent(OrderResultEventArgs e)
        {
            if (OrderResultEvent != null)
            {
                OrderResultEvent(this, e);
            }
        }

        public void Start()
        {
            status = true;
            switch (mode)
            {
                case 0:
                    //捡漏模式，只有小号参与捡漏，而且每次固定只使用一个号，一张一张抢
                    foreach (Account account in accountsList)
                    {
                        if (account.importance == 1 && status)
                        {
                            if (account.Login())
                            {
                                Int32 errorCode = 0;
                                //只要不是帐号已经买满了数量，就循环不断的买
                                while (errorCode != 888 && status)
                                {
                                    errorCode = account.Buy(id, 1, type);
                                    OrderResultEventArgs ev = new OrderResultEventArgs(account.username, errorCode, errorCodeList[errorCode]);
                                    DispatchOrderCompleteEvent(ev);
                                    delay(this.delayTime);
                                }
                                continue;
                            }
                        }
                    }
                    break;

                case 1:
                    //定量模式，一般用在开票的时候，指定一定数量的小号参与购买，限购多少就买多少
                    foreach (Account account in accountsList)
                    {
                        if (account.importance == 1 && status)
                        {
                            if (account.Login())
                            {
                                Int32 errorCode = 0;
                                //一次性抢限购数量上限的数量
                                while (errorCode != 888 && status)
                                {
                                    errorCode = account.Buy(id, 2, type);
                                    OrderResultEventArgs ev = new OrderResultEventArgs(account.username, errorCode, errorCodeList[errorCode]);
                                    DispatchOrderCompleteEvent(ev);
                                    delay(this.delayTime);
                                }
                                //这里有BUG
                                accountsNum--;
                                if (accountsNum > 0)
                                {
                                    continue;
                                }
                                else
                                {
                                    return;
                                }
                            }
                        }
                    }
                    break;

                case 2:
                    //大号购买模式，一般用在开票的时候，指定一定数量的大号参与购买，一张一张买，买到上限为止
                    foreach (Account account in accountsList)
                    {
                        if (account.username == this.accountUserName && status)
                        {
                            if (account.Login())
                            {
                                Int32 errorCode = 0;
                                //一次性抢限购数量上限的数量
                                while (errorCode != 888 && status)
                                {
                                    errorCode = account.Buy(id, 2, type);
                                    OrderResultEventArgs ev = new OrderResultEventArgs(account.username, errorCode, errorCodeList[errorCode]);
                                    DispatchOrderCompleteEvent(ev);
                                    delay(this.delayTime);
                                }
                                continue;
                            }
                        }
                    }
                    break;

                default:
                    break;
            }
        }

        private void delay(Int32 millisecends)
        {
            DateTime tempTime = DateTime.Now;
            while (tempTime.AddMilliseconds(millisecends).CompareTo(DateTime.Now) > 0)
            {
                Application.DoEvents();
            }
        }
    }
}
