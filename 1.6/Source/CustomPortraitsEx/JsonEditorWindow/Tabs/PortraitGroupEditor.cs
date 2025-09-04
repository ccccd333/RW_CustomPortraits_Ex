using Foxy.CustomPortraits.CustomPortraitsEx.Repository;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace Foxy.CustomPortraits.CustomPortraitsEx.JsonEditorWindow.Tabs
{
    public class PortraitGroupEditor : TabBase
    {
        int stage = 0;
        Refs refs;
        bool edited_cache_flag = false;
        bool edited_idle_cache_flag = false;
        bool edited_dead_cache_flag = false;
        List<string> temp_files = new List<string>();
        TextureMeta temp_texture_meta = new TextureMeta();
        string temp_st_display_duration = "6.0";
        string temp_st_idle_display_duration = "6.0";
        string temp_st_dead_display_duration = "6.0";
        List<string> error_list = new List<string>();

        TextureMeta temp_texture_idle_meta = new TextureMeta();
        TextureMeta temp_texture_dead_meta = new TextureMeta();

        // グループ名はGroupEditor側のresult_edit_target_group_name
        // 今回のグループ名に対するテクスチャ群
        public TextureMeta result_texture_meta = new TextureMeta();
        // アイドル用のテクスチャ群
        public TextureMeta result_texture_idle_meta = new TextureMeta();
        // 死亡用のテクスチャ群
        public TextureMeta result_texture_dead_meta = new TextureMeta();

        public void Draw(Rect inRect, string edit_target_group_name, string selected_preset_name)
        {
            Listing_Standard listing = Begin(inRect);
            listing.Label(Helper.Label("RCP_PG_Desc1"));
            listing.Label(Helper.Label("RCP_PG_Desc2"));
            listing.Label(Helper.Label("RCP_PG_Desc3"));
            listing.Label(Helper.Label("RCP_PG_Desc4"));
            listing.Label(Helper.Label("RCP_PG_Desc5"));
            listing.GapLine();

            if (edit_target_group_name == "")
            {
                listing.Label(Helper.Label("RCP_PG_Desc6"));
            }
            else
            {
                switch (stage)
                {
                    case 0:
                        EditPortraitGroup(listing, edit_target_group_name, selected_preset_name);
                        break;
                    case 1:
                        EditIdlePortrait(listing, selected_preset_name);
                        break;
                    case 2:
                        EditDeadPortrait(listing, selected_preset_name);
                        break;
                    case 3:
                        EndEditing(listing);
                        break;
                }

                SetStage();
            }

            End(listing);
        }

        public void Reset()
        {
            stage = 0;
            call_id = "";
            refs = null;
            edited_cache_flag = false;
            edited_idle_cache_flag = false;
            edited_dead_cache_flag = false;

            temp_files.Clear();
            temp_texture_meta = new TextureMeta();

            temp_texture_idle_meta = new TextureMeta();
            temp_texture_dead_meta = new TextureMeta();

            result_texture_meta = new TextureMeta();
            result_texture_idle_meta = new TextureMeta();
            result_texture_dead_meta = new TextureMeta();
            temp_st_display_duration = "6.0";
            temp_st_idle_display_duration = "6.0";
            temp_st_dead_display_duration = "6.0";

            error_list.Clear();
        }

        private void EditPortraitGroup(Listing_Standard listing, string edit_target_group_name, string selected_preset_name)
        {
            listing.Label(Helper.Label("RCP_PG_EditPortraitGroupDesc1"));
            listing.GapLine();

            Rect back_rect = listing.GetRect(30f);
            if (Widgets.ButtonText(back_rect.RightPart(0.55f), Helper.Label("RCP_B_Back")))
            {
                call_id = "back";
            }

            listing.GapLine();

            if (PortraitCacheEx.Refs.ContainsKey(selected_preset_name))
            {
                refs = PortraitCacheEx.Refs[selected_preset_name];
                if (refs.txs.ContainsKey(edit_target_group_name))
                {

                    listing.Label($"{Helper.Label("RCP_PG_EditPortraitGroupDesc2")}{edit_target_group_name}");
                    if (!edited_cache_flag)
                    {
                        var tx = refs.txs[edit_target_group_name];
                        temp_texture_meta = new TextureMeta(tx);
                        //tx.file_path_first;
                        temp_st_display_duration = temp_texture_meta.display_duration.ToString();
                        //Log.Message($"{temp_texture_meta.d} {temp_texture_meta.file_base_path} {temp_texture_meta.file_path_first} {temp_texture_meta.file_path_second} {temp_texture_meta.IsAnimation}");
                    }

                    edited_cache_flag = true;

                    listing.GapLine();
                }
            }

            PortraitEditTemplate(listing, temp_texture_meta, ref result_texture_meta, selected_preset_name, edit_target_group_name, ref temp_st_display_duration, "edit portrait group");

        }

        private void EditIdlePortrait(Listing_Standard listing, string selected_preset_name)
        {
            listing.Label(Helper.Label("RCP_PG_EditIdlePortraitDesc1"));
            listing.Label(Helper.Label("RCP_PG_EditIdlePortraitDesc2"));
            listing.Label(Helper.Label("RCP_PG_EditIdlePortraitDesc3"));
            listing.Label(Helper.Label("RCP_PG_EditIdlePortraitDesc4"));
            listing.GapLine();

            Rect back_rect = listing.GetRect(30f);
            if (Widgets.ButtonText(back_rect.RightPart(0.55f), Helper.Label("RCP_B_Back")))
            {
                call_id = "edit portrait group->back";
            }

            listing.GapLine();
            if (PortraitCacheEx.Refs.ContainsKey(selected_preset_name))
            {
                if (refs.txs.ContainsKey("Idle"))
                {

                    listing.Label($"{Helper.Label("RCP_PG_EditIdlePortraitDesc5")}Idle");
                    if (!edited_idle_cache_flag)
                    {
                        var tx = refs.txs["Idle"];
                        temp_texture_idle_meta = new TextureMeta(tx);
                        //tx.file_path_first;
                        temp_st_idle_display_duration = temp_texture_idle_meta.display_duration.ToString();
                        //Log.Message($"{temp_texture_meta.d} {temp_texture_meta.file_base_path} {temp_texture_meta.file_path_first} {temp_texture_meta.file_path_second} {temp_texture_meta.IsAnimation}");
                    }

                    edited_idle_cache_flag = true;

                    listing.GapLine();
                }
            }

            PortraitEditTemplate(listing, temp_texture_idle_meta, ref result_texture_idle_meta, selected_preset_name, "Idle", ref temp_st_idle_display_duration, "edit portrait group->edit idle portrait");
        }

        private void EditDeadPortrait(Listing_Standard listing, string selected_preset_name)
        {
            listing.Label(Helper.Label("RCP_PG_EditDeadPortraitDesc1"));
            listing.Label(Helper.Label("RCP_PG_EditDeadPortraitDesc2"));
            listing.Label(Helper.Label("RCP_PG_EditDeadPortraitDesc3"));
            listing.GapLine();

            Rect back_rect = listing.GetRect(30f);
            if (Widgets.ButtonText(back_rect.RightPart(0.55f), Helper.Label("RCP_B_Back")))
            {
                call_id = "edit portrait group->edit dead->back";
            }

            listing.GapLine();
            if (PortraitCacheEx.Refs.ContainsKey(selected_preset_name))
            {
                if (refs.txs.ContainsKey("Dead"))
                {

                    listing.Label($"{Helper.Label("RCP_PG_EditDeadPortraitDesc4")}Dead");
                    if (!edited_dead_cache_flag)
                    {
                        var tx = refs.txs["Dead"];
                        temp_texture_dead_meta = new TextureMeta(tx);
                        //tx.file_path_first;
                        temp_st_dead_display_duration = temp_texture_dead_meta.display_duration.ToString();
                        //Log.Message($"{temp_texture_meta.d} {temp_texture_meta.file_base_path} {temp_texture_meta.file_path_first} {temp_texture_meta.file_path_second} {temp_texture_meta.IsAnimation}");
                    }

                    edited_dead_cache_flag = true;

                    listing.GapLine();
                }
            }

            PortraitEditTemplate(listing, temp_texture_dead_meta, ref result_texture_dead_meta, selected_preset_name, "Dead", ref temp_st_dead_display_duration, "edit end");
        }

        private void EndEditing(Listing_Standard listing)
        {
            Rect back_rect = listing.GetRect(30f);
            if (Widgets.ButtonText(back_rect.RightPart(0.55f), Helper.Label("RCP_B_Back")))
            {
                call_id = "edit end->back";
            }

            listing.GapLine();

            listing.Label(Helper.Label("RCP_PG_EndEditingDesc1"));
            listing.Label(Helper.Label("RCP_PG_EndEditingDesc2"));

            listing.GapLine();
            listing.Label($"{Helper.Label("RCP_PG_EndEditingDesc3")}{result_texture_meta.file_path}");
            listing.Label($"{Helper.Label("RCP_PG_EndEditingDesc4")}{result_texture_meta.IsAnimation}");
            listing.Label($"{Helper.Label("RCP_PG_EndEditingDesc5")}{result_texture_meta.display_duration}");
        }

        private void PortraitEditTemplate(Listing_Standard listing, TextureMeta meta, ref TextureMeta result_meta, string selected_preset_name, string edit_target_group_name, ref string work_display_duration, string id)
        {
            listing.Label(Helper.Label("RCP_PG_PortraitEditTemplateDesc1"));
            listing.Label(Helper.Label("RCP_PG_PortraitEditTemplateDesc2"));
            listing.Label(Helper.Label("RCP_PG_PortraitEditTemplateDesc3"));

            Rect base_path_rect = listing.GetRect(30f);
            meta.file_base_path = $"{selected_preset_name}/{edit_target_group_name}/";
            listing.Label(meta.file_base_path);
            //temp_texture_meta.file_base_path = Widgets.TextField(base_path_rect, temp_texture_meta.file_base_path);

            listing.GapLine();

            listing.Label(Helper.Label("RCP_PG_PortraitEditTemplateDesc4"));

            Rect is_anim_rect = listing.GetRect(30f);

            // ラベル
            Widgets.Label(is_anim_rect.LeftPart(0.6f), Helper.Label("RCP_PG_PortraitEditTemplateDesc5"));

            // SELECT ボタン
            if (Widgets.ButtonText(is_anim_rect.RightPart(0.55f).LeftPart(0.7f), Helper.Label("RCP_B_Select")))
            {
                meta.IsAnimation = !meta.IsAnimation;
            }

            // チェックボックス
            bool is_anim = meta.IsAnimation;
            Rect checkbox_rect = is_anim_rect.RightPart(0.15f);
            Widgets.Checkbox(checkbox_rect.x, checkbox_rect.y, ref is_anim);

            listing.GapLine();
            listing.Label(Helper.Label("RCP_PG_PortraitEditTemplateDesc6"));

            Rect input_f_rect = listing.GetRect(30f);
            float rv = meta.display_duration;
            string display_dur_buff = work_display_duration;
            Widgets.TextFieldNumeric<float>(input_f_rect, ref rv, ref display_dur_buff);
            meta.display_duration = rv;
            work_display_duration = display_dur_buff;

            listing.GapLine();
            if (meta.IsAnimation)
            {
                listing.Label(Helper.Label("RCP_PG_PortraitEditTemplateDesc7"));
                listing.Label(Helper.Label("RCP_PG_PortraitEditTemplateDesc8"));

                meta.file_path_first = "1";
                listing.Label($"From==>{meta.file_path_first}");


                listing.Label($"To==>{meta.file_path_second}");

                Rect input_to_rect = listing.GetRect(30f);
                int to_tex = 2;
                if (!int.TryParse(meta.file_path_second, out to_tex)) { to_tex = 2; }

                Widgets.TextFieldNumeric<int>(input_to_rect, ref to_tex, ref meta.file_path_second);
                meta.file_path_second = to_tex.ToString();

                listing.GapLine();

                listing.Label(Helper.Label("RCP_PG_PortraitEditTemplateDesc9"));
                meta.d = ".dds";
                listing.Label($"{Helper.Label("RCP_PG_PortraitEditTemplateDesc10")}==>{meta.d}");

                meta.file_path = meta.file_base_path + meta.file_path_first + meta.d + "~" + meta.file_path_second + meta.d;
            }
            else
            {
                listing.Label(Helper.Label("RCP_PG_PortraitEditTemplateDesc11"));
                listing.Label(Helper.Label("RCP_PG_PortraitEditTemplateDesc12"));

                meta.file_path_first = "1";
                listing.Label($"From==>{meta.file_path_first}");

                listing.Label(Helper.Label("RCP_PG_PortraitEditTemplateDesc13"));
                listing.Label($"{Helper.Label("RCP_PG_PortraitEditTemplateDesc14")}==>{meta.d}");

                Rect is_ext_dds_rect = listing.GetRect(30f);
                Widgets.Label(is_ext_dds_rect.LeftPart(0.6f), ".dds");

                // SELECT ボタン
                if (Widgets.ButtonText(is_ext_dds_rect.RightPart(0.55f).LeftPart(0.7f), Helper.Label("RCP_B_Select")))
                {
                    meta.d = ".dds";
                }

                bool is_dds = meta.d == ".dds" ? true : false;
                // チェックボックス
                Rect checkbox_rect1 = is_ext_dds_rect.RightPart(0.15f);
                Widgets.Checkbox(checkbox_rect1.x, checkbox_rect1.y, ref is_dds);

                Rect is_ext_png_rect = listing.GetRect(30f);
                Widgets.Label(is_ext_png_rect.LeftPart(0.6f), ".png");

                // SELECT ボタン
                if (Widgets.ButtonText(is_ext_png_rect.RightPart(0.55f).LeftPart(0.7f), Helper.Label("RCP_B_Select")))
                {
                    meta.d = ".png";
                }

                bool is_png = meta.d == ".png" ? true : false;
                // チェックボックス
                Rect checkbox_rect2 = is_ext_png_rect.RightPart(0.15f);
                Widgets.Checkbox(checkbox_rect2.x, checkbox_rect2.y, ref is_png);

                meta.file_path = meta.file_base_path + meta.file_path_first + meta.d;
            }
            listing.GapLine();

            if (error_list.Count > 0)
            {
                foreach (var error in error_list)
                {
                    listing.Label(error);
                }
            }

            Rect enter_rect = listing.GetRect(30f);
            if (Widgets.ButtonText(enter_rect.RightPart(0.55f).LeftPart(0.7f), Helper.Label("RCP_B_Enter")))
            {
                bool check = true;
                error_list.Clear();
                if (meta.IsAnimation)
                {
                    if (meta.d != ".dds")
                    {
                        error_list.Add("[ERROR] animationなのにDDSじゃない[これ見かけたら報告ください]");
                        check = false;
                    }

                    if (meta.file_base_path == "")
                    {
                        error_list.Add("[ERROR] file_base_pathがない[これ見かけたら報告ください]");
                        check = false;
                    }

                    if (meta.file_path_first != "1")
                    {
                        error_list.Add("[ERROR] file_path_firstが1以外になってる[これ見かけたら報告ください]");
                        check = false;

                    }
                    int outint = -1;
                    if (int.TryParse(meta.file_path_second, out outint))
                    {
                        if (outint < 1)
                        {
                            error_list.Add("[ERROR] from-toのto部分がfrom部分より小さい値になってます");
                            check = false;
                        }
                    }

                    if (meta.display_duration < 0.01f)
                    {
                        error_list.Add("[ERROR] 切り替わり時間は1秒以上に設定してください");
                        check = false;
                    }
                }
                else
                {

                    if (meta.d != ".dds" && meta.d != ".png")
                    {
                        error_list.Add("[ERROR] 拡張子がDDSとPNG以外[これ見かけたら報告ください]");
                        check = false;

                    }

                    if (meta.file_base_path == "")
                    {
                        error_list.Add("[ERROR] file_base_pathがない[これ見かけたら報告ください]");
                        check = false;
                    }

                    if (meta.file_path_first != "1")
                    {
                        error_list.Add("[ERROR] file_path_firstが1以外になってる[これ見かけたら報告ください]");
                        check = false;
                    }

                    if (meta.display_duration < 0.01f)
                    {
                        error_list.Add("[ERROR] 切り替わり時間は1秒以上に設定してください");
                        check = false;
                    }
                }

                if (check)
                {
                    call_id = id;
                    result_meta = new TextureMeta(meta);
                }
            }
        }

        private void SetStage()
        {
            if (stage == 0)
            {
                if (call_id == "edit portrait group")
                {
                    stage = 1;
                }
                else if (call_id == "back")
                {
                    Reset();
                    stage = 0;
                }
            }
            else if (stage == 1)
            {
                if (call_id == "edit portrait group->edit idle portrait")
                {
                    stage = 2;
                }
                else if (call_id == "edit portrait group->back")
                {
                    Reset();
                    stage = 0;
                }
            }
            else if (stage == 2)
            {
                if (call_id == "edit end")
                {
                    stage = 3;
                }
                else if (call_id == "edit portrait group->edit dead->back")
                {
                    Reset();
                    stage = 0;
                }
            }
            else if (stage == 3)
            {
                if (call_id == "edit end->back")
                {
                    Reset();
                    stage = 0;
                }
            }
        }


    }
}
