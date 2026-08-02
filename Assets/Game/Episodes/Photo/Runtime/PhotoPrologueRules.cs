using System;

namespace Jam.Episodes.Photo
{
    public enum PhotoPrologueStep
    {
        RoomSecret = 0,
        RoomPhoto = 1,
        MotherDialogue = 2,
        MailboxHunt = 3,
        MailboxPublication = 4,
        MailboxReaction = 5,
        AirportPhoto = 6,
        BorderControl = 7,
        Summary = 8,
        Complete = 9
    }

    public enum PhotoSecretChoice
    {
        None = 0,
        TheyWillNotKnow = 1,
        TheyAlreadyKnow = 2,
        LetThemKnow = 3
    }

    public enum PhotoRoomShotChoice
    {
        None = 0,
        Honest = 1,
        Wings = 2
    }

    public enum PhotoMotherReply
    {
        None = 0,
        Honest = 1,
        ProtectiveLie = 2
    }

    public enum PhotoProloguePath
    {
        Undecided = 0,
        Honesty = 1,
        Recognition = 2,
        Balance = 3
    }

    public enum PhotoMailboxPublication
    {
        None = 0,
        Wings = 1,
        Honest = 2,
        Balance = 3
    }

    public enum PhotoBorderReply
    {
        None = 0,
        Honest = 1,
        Recognition = 2
    }

    public static class PhotoPrologueRules
    {
        public const int MinimumScaleValue = 0;
        public const int MaximumScaleValue = 100;
        public const int InitialHonesty = 20;
        public const int InitialRecognition = 20;
        public const int SummonsDetailBit = 0b01;
        public const int ButterflyDetailBit = 0b10;
        public const int AllMailboxDetailsMask = SummonsDetailBit | ButterflyDetailBit;

        public static bool ApplySecretChoice(PhotoPrologueProgress progress, PhotoSecretChoice choice)
        {
            if (progress == null
                || progress.step != PhotoPrologueStep.RoomSecret
                || progress.secretChoice != PhotoSecretChoice.None
                || choice == PhotoSecretChoice.None)
            {
                return false;
            }

            progress.secretChoice = choice;
            if (choice == PhotoSecretChoice.TheyAlreadyKnow)
            {
                AddRecognition(progress, 20);
            }
            else if (choice == PhotoSecretChoice.LetThemKnow)
            {
                AddHonesty(progress, 20);
            }

            progress.step = PhotoPrologueStep.RoomPhoto;
            return true;
        }

        public static bool ApplyRoomShot(PhotoPrologueProgress progress, PhotoRoomShotChoice choice)
        {
            if (progress == null
                || progress.step != PhotoPrologueStep.RoomPhoto
                || progress.roomShotChoice != PhotoRoomShotChoice.None
                || choice == PhotoRoomShotChoice.None)
            {
                return false;
            }

            progress.roomShotChoice = choice;
            if (choice == PhotoRoomShotChoice.Honest)
            {
                AddHonesty(progress, 20);
            }
            else
            {
                AddRecognition(progress, 20);
            }

            progress.step = PhotoPrologueStep.MotherDialogue;
            return true;
        }

        public static bool ApplyMotherReply(PhotoPrologueProgress progress, PhotoMotherReply choice)
        {
            if (progress == null
                || progress.step != PhotoPrologueStep.MotherDialogue
                || progress.motherReply != PhotoMotherReply.None
                || choice == PhotoMotherReply.None)
            {
                return false;
            }

            if (choice == PhotoMotherReply.Honest && progress.honesty < 20)
            {
                return false;
            }

            progress.motherReply = choice;
            if (choice == PhotoMotherReply.Honest)
            {
                AddHonesty(progress, -20);
            }
            else
            {
                AddRecognition(progress, 20);
            }

            progress.step = PhotoPrologueStep.MailboxHunt;
            return true;
        }

        public static bool DiscoverMailboxDetail(PhotoPrologueProgress progress, int detailBit)
        {
            if (progress == null
                || progress.step != PhotoPrologueStep.MailboxHunt
                || (detailBit & AllMailboxDetailsMask) == 0)
            {
                return false;
            }

            var previous = progress.mailboxDetailsMask;
            progress.mailboxDetailsMask |= detailBit & AllMailboxDetailsMask;
            return previous != progress.mailboxDetailsMask;
        }

        public static bool BeginMailboxPublication(PhotoPrologueProgress progress)
        {
            if (progress == null
                || progress.step != PhotoPrologueStep.MailboxHunt
                || progress.mailboxDetailsMask == 0)
            {
                return false;
            }

            progress.step = PhotoPrologueStep.MailboxPublication;
            return true;
        }

        public static bool ApplyMailboxPublication(
            PhotoPrologueProgress progress,
            PhotoMailboxPublication publication)
        {
            if (progress == null
                || progress.step != PhotoPrologueStep.MailboxPublication
                || progress.mailboxPublication != PhotoMailboxPublication.None
                || publication == PhotoMailboxPublication.None
                || !HasRequiredDetails(progress, publication))
            {
                return false;
            }

            progress.mailboxPublication = publication;
            switch (publication)
            {
                case PhotoMailboxPublication.Wings:
                    AddRecognition(progress, 50);
                    progress.path = PhotoProloguePath.Recognition;
                    break;
                case PhotoMailboxPublication.Honest:
                    AddHonesty(progress, 50);
                    progress.path = PhotoProloguePath.Honesty;
                    break;
                case PhotoMailboxPublication.Balance:
                    AddHonesty(progress, 25);
                    AddRecognition(progress, 25);
                    progress.path = PhotoProloguePath.Balance;
                    break;
                default:
                    return false;
            }

            progress.publicationCommitted = true;
            progress.step = PhotoPrologueStep.MailboxReaction;
            return true;
        }

        public static bool ContinueToAirport(PhotoPrologueProgress progress)
        {
            if (progress == null || progress.step != PhotoPrologueStep.MailboxReaction)
            {
                return false;
            }

            progress.step = PhotoPrologueStep.AirportPhoto;
            return true;
        }

        public static bool ResolveAirportPhoto(PhotoPrologueProgress progress, bool takePhoto)
        {
            if (progress == null || progress.step != PhotoPrologueStep.AirportPhoto)
            {
                return false;
            }

            progress.airportPhotoResolved = true;
            progress.airportPhotoTaken = takePhoto;
            progress.step = PhotoPrologueStep.BorderControl;
            return true;
        }

        public static bool IsBorderReplyAvailable(PhotoPrologueProgress progress, PhotoBorderReply reply)
        {
            if (progress == null || reply == PhotoBorderReply.None)
            {
                return false;
            }

            return progress.path switch
            {
                PhotoProloguePath.Honesty => reply == PhotoBorderReply.Honest,
                PhotoProloguePath.Recognition => reply == PhotoBorderReply.Recognition,
                PhotoProloguePath.Balance => true,
                _ => false
            };
        }

        public static bool ApplyBorderReply(PhotoPrologueProgress progress, PhotoBorderReply reply)
        {
            if (progress == null
                || progress.step != PhotoPrologueStep.BorderControl
                || progress.borderReply != PhotoBorderReply.None
                || !IsBorderReplyAvailable(progress, reply))
            {
                return false;
            }

            progress.borderReply = reply;
            progress.step = PhotoPrologueStep.Summary;
            return true;
        }

        public static bool Complete(PhotoPrologueProgress progress)
        {
            if (progress == null || progress.step != PhotoPrologueStep.Summary)
            {
                return false;
            }

            progress.step = PhotoPrologueStep.Complete;
            progress.completed = true;
            return true;
        }

        public static void ClampScales(PhotoPrologueProgress progress)
        {
            if (progress == null)
            {
                return;
            }

            progress.honesty = Math.Clamp(progress.honesty, MinimumScaleValue, MaximumScaleValue);
            progress.recognition = Math.Clamp(progress.recognition, MinimumScaleValue, MaximumScaleValue);
        }

        private static bool HasRequiredDetails(
            PhotoPrologueProgress progress,
            PhotoMailboxPublication publication)
        {
            return publication switch
            {
                PhotoMailboxPublication.Wings =>
                    (progress.mailboxDetailsMask & ButterflyDetailBit) != 0,
                PhotoMailboxPublication.Honest =>
                    (progress.mailboxDetailsMask & SummonsDetailBit) != 0,
                PhotoMailboxPublication.Balance =>
                    progress.mailboxDetailsMask == AllMailboxDetailsMask,
                _ => false
            };
        }

        private static void AddHonesty(PhotoPrologueProgress progress, int delta)
        {
            progress.honesty = Math.Clamp(
                progress.honesty + delta,
                MinimumScaleValue,
                MaximumScaleValue);
        }

        private static void AddRecognition(PhotoPrologueProgress progress, int delta)
        {
            progress.recognition = Math.Clamp(
                progress.recognition + delta,
                MinimumScaleValue,
                MaximumScaleValue);
        }
    }
}
